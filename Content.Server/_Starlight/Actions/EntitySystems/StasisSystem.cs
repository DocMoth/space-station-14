using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Timing;
using Content.Shared.Mobs;
using Content.Shared.FixedPoint;
using Content.Shared._Starlight.Actions.EntitySystems;
using Content.Shared._Starlight.Actions.Components;
using Content.Shared._Starlight.Actions.Events;
using Robust.Shared.Player;
using Content.Shared.Damage.Systems;

namespace Content.Server._Starlight.Actions.EntitySystems;

public sealed partial class StasisSystem : SharedStasisSystem
{
    [Dependency] private SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StasisComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StasisComponent, ComponentShutdown>(OnCompRemove);
        SubscribeLocalEvent<StasisComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<StasisComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<StasisComponent, PrepareStasisActionEvent>(OnPrepareStasisStart);
        SubscribeLocalEvent<StasisComponent, EnterStasisActionEvent>(OnEnterStasisStart);
        SubscribeLocalEvent<StasisComponent, ExitStasisActionEvent>(OnExitStasisStart);
    }

    private void OnMapInit(EntityUid uid, StasisComponent comp, MapInitEvent args)
        => _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);

    private void OnCompRemove(EntityUid uid, StasisComponent comp, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
    }

    private void OnMobStateChanged(Entity<StasisComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead && ent.Comp.IsInStasis)
            RaiseLocalEvent(args.Target, new ExitStasisActionEvent());
    }

    private void OnDamageModify(Entity<StasisComponent> ent, ref DamageModifyEvent args)
    {
        // TODO: this might mean like hitting yourself with a bomb or something while in stasis wont resist damage.
        if (!ent.Comp.IsInStasis || args.Origin == ent)
            return;

        // Reduce all positive damage.
        var updatedDamage = new DamageSpecifier();
        FixedPoint2 flatDamage = 0;
        foreach (var damage in args.Damage.DamageDict)
        {
            var scaler = ent.Comp.StasisDamageReduction;
            if(damage.Value > 0)
            {
                flatDamage += damage.Value;
            }
            updatedDamage.DamageDict[damage.Key] = damage.Value > 0 ? scaler * damage.Value : damage.Value;
        }

        // Remove shell health. Not including the stasis damage reduction.
        ent.Comp.DamageTaken += flatDamage;
        // Total damage stasis can take, ends the stasis early if it is met.
        if(ent.Comp.DamageTaken >= ent.Comp.StasisHealth)
        {
            RaiseLocalEvent(ent.Owner, new ExitStasisActionEvent());
        }

        args.Damage = updatedDamage;
    }

    private void OnPrepareStasisStart(EntityUid uid, StasisComponent comp, PrepareStasisActionEvent args)
    {
        _actionsSystem.RemoveAction(uid, comp.EnterStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.ExitStasisActionEntity, comp.ExitStasisAction);
        _actionsSystem.SetCooldown(comp.ExitStasisActionEntity, comp.StasisEnterEffectLifetime);

        // Send animation event to all clients
        var ev = new StasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates), StasisAnimationType.Prepare);
        RaiseNetworkEvent(ev, Filter.Pvs(uid, entityManager: EntityManager));

        // TODO: refactor this to not use timers
        // Schedule the enter stasis event after delay
        Timer.Spawn(comp.StasisEnterEffectLifetime, () =>
        {
            if (!HasComp<StasisComponent>(uid))
                return;

            var enterEv = new EnterStasisActionEvent();
            RaiseLocalEvent(uid, enterEv);
        });
    }

    private void OnEnterStasisStart(EntityUid uid, StasisComponent comp, EnterStasisActionEvent args)
    {
        comp.IsInStasis = true;
        comp.IsVisible = false; // Entity becomes invisible when entering stasis to better show the effect
        comp.DamageTaken = FixedPoint2.Zero;

        EntityManager.AddComponents(uid, comp.StasisComponents);

        Dirty(uid, comp);

        // Send animation event to all clients
        var ev = new StasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates), StasisAnimationType.Enter);
        RaiseNetworkEvent(ev, Filter.Pvs(uid, entityManager: EntityManager));
    }

    private void OnExitStasisStart(EntityUid uid, StasisComponent comp, ExitStasisActionEvent args)
    {
        comp.IsInStasis = false;
        comp.IsVisible = true; // Entity becomes visible when exiting stasis
        comp.DamageTaken = FixedPoint2.Zero;

        EntityManager.RemoveComponents(uid, comp.StasisComponents);

        Dirty(uid, comp);

        _actionsSystem.RemoveAction(uid, comp.ExitStasisActionEntity);
        _actionsSystem.AddAction(uid, ref comp.EnterStasisActionEntity, comp.EnterStasisAction);
        _actionsSystem.SetCooldown(comp.EnterStasisActionEntity, comp.StasisCooldown);

        // Send animation event to all clients
        var ev = new StasisAnimationEvent(GetNetEntity(uid), GetNetCoordinates(Transform(uid).Coordinates),
            StasisAnimationType.Exit);
        RaiseNetworkEvent(ev, Filter.Pvs(uid, entityManager: EntityManager));
    }
}
