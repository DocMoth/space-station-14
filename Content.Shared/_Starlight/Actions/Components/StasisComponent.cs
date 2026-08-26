using Content.Shared._Starlight.Actions.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Actions.Components;

/// <summary>
/// Component that allows an entity to enter and exit stasis.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause, Access(typeof(SharedStasisSystem))]
public sealed partial class StasisComponent : Component
{
    /// <summary>
    /// Whether the entity is currently in stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsInStasis;

    /// <summary>
    /// How much damage the stasis has taken.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DamageTaken;

    /// <summary>
    /// Whether the entity should be visible. This is synced to ensure proper PVS handling.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsVisible = true;

    /// <summary>
    /// The entity needed to actually preform stasis. This will be granted (and removed) upon the entity's creation.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId EnterStasisAction;

    /// <summary>
    /// The second entity needed to preform stasis. This is used to leave stasis.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId ExitStasisAction;

    [DataField, AutoNetworkedField]
    public EntityUid? ExitStasisActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? EnterStasisActionEntity;

    /// <summary>
    /// The cooldown time for the stasis ability, in seconds.
    /// </summary>
    [DataField]
    public TimeSpan StasisCooldown = TimeSpan.FromSeconds(300);

    /// <summary>
    /// The total amount of health the stasis has before breaking.
    /// </summary>
    [DataField]
    public float StasisHealth = 50f;

    /// <summary>
    /// Flat percentage damage resistance against ALL positive damage taken (Healing is not effected)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StasisDamageReduction = 0.5f;

    /// <summary>
    /// The prototype ID of the stasis effect to spawn when entering stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId StasisEnterEffect = "EffectNanitesEnter";

    /// <summary>
    /// The lifetime of the entering stasis effect in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan StasisEnterEffectLifetime = TimeSpan.FromSeconds(1.7);

    /// <summary>
    /// The sound to play when entering stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier StasisEnterSound =  new SoundPathSpecifier("/Audio/_Starlight/Misc/alien_teleport.ogg");

    /// <summary>
    /// The prototype ID of the stasis effect to spawn when exiting stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId StasisExitEffect = "EffectNanitesExit";

    /// <summary>
    /// The lifetime of the exit stasis effect in seconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StasisExitEffectLifetime = 1.7f;

    /// <summary>
    /// The sound to play when exiting stasis.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier StasisExitSound = new SoundPathSpecifier("/Audio/_Starlight/Misc/alien_teleport.ogg");

    /// <summary>
    /// The prototype ID of the stasis effect to spawn when stasis is currently in use.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId StasisContinuousEffect = "EffectNanitesCurrent";

    /// <summary>
    /// The entity reference for the continuous stasis effect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ContinuousEffectEntity;

    /// <summary>
    /// Client-side reference to the continuous stasis effect entity.
    /// This is used to properly track and clean up the visual effect.
    /// </summary>
    [DataField] public EntityUid? ClientContinuousEffectEntity;

    /// <summary>
    /// Client-side reference to the enter stasis effect entity.
    /// This is used to properly track and clean up the visual effect.
    /// </summary>
    [DataField] public EntityUid? ClientEnterEffectEntity;

    /// <summary>
    /// Components applied when you enter stasis.
    /// </summary>
    [DataField]
    public ComponentRegistry StasisComponents = new();
}
