using Content.Shared.Power;
using Content.Shared.Radio; // Pinwheel - traitor sabotage
using Robust.Shared.Audio; // Pinwheel - traitor sabotage
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes; // Pinwheel - traitor sabotage
using Robust.Shared.Timing; // Pinwheel - gravity drift

namespace Content.Shared.Gravity;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GravityGeneratorComponent : Component
{
    [DataField] public float LightRadiusMin { get; set; }
    [DataField] public float LightRadiusMax { get; set; }

    /// <summary>
    /// A map of the sprites used by the gravity generator given its status.
    /// </summary>
    [DataField, Access(typeof(SharedGravitySystem))]
    public Dictionary<PowerChargeStatus, string> SpriteMap = [];

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is starting up.
    /// </summary>
    [DataField]
    public string CoreStartupState = "startup";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is idle.
    /// </summary>
    [DataField]
    public string CoreIdleState = "idle";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is activating.
    /// </summary>
    [DataField]
    public string CoreActivatingState = "activating";

    /// <summary>
    /// The sprite used by the core of the gravity generator when the gravity generator is active.
    /// </summary>
    [DataField]
    public string CoreActivatedState = "activated";

    /// <summary>
    /// Is the gravity generator currently "producing" gravity?
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(SharedGravityGeneratorSystem))]
    public bool GravityActive = false;

    // Pinwheel-stt - gravity drift
    /// <summary>
    /// Should entities drift in this generator's direction
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DriftEnabled = false;

    /// <summary>
    /// How often the drift impulse is applied
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DriftRate = TimeSpan.FromSeconds(1);

    /// <summary>
    /// When the next drift impulse will be applied
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DriftNext;
    // Pinwheel-end - gravity drift

    // Pinwheel-stt - traitor sabotage
    /// <summary>
    /// The radio channel to whine on when something goes wrong
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> MessageChannel = "Engineering";

    /// <summary>
    /// Message to use when the maintenance panel is taken off
    /// </summary>
    [DataField]
    public LocId MessageOpen = "sabotage-message-open-gravity";

    /// <summary>
    /// Message to use when the sabotage doodad is jammed in
    /// </summary>
    [DataField]
    public LocId MessageStart = "sabotage-message-start-gravity";

    /// <summary>
    /// Message to use when the sabotage doohickey is removed
    /// </summary>
    [DataField]
    public LocId MessageStop = "sabotage-message-stop-gravity";

    /// <summary>
    /// Message to use when the sabotage macguffin does its thing
    /// </summary>
    [DataField]
    public LocId MessageComplete = "sabotage-message-complete-gravity";

    /// <summary>
    /// Message to use when warning of an imminent quake
    /// </summary>
    [DataField]
    public LocId MessageQuake = "sabotage-message-quake-gravity";

    /// <summary>
    /// Sound played with sabotage announcement
    /// </summary>
    [DataField]
    public SoundSpecifier? SabotageAnnouncementSound;

    /// <summary>
    /// Is the sabotage complete. Checked by the objective
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SabotageComplete = false;

    /// <summary>
    /// How many seconds in advance to warn of a quake
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan QuakeWarning = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Have we warned of the quake yet
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool QuakeWarned = false;

    /// <summary>
    /// When the next quake will happen
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan QuakeNext;

    /// <summary>
    /// Minimum time between quakes
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan QuakeMin = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Maximum time between quakes
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan QuakeMax = TimeSpan.FromSeconds(840);

    /// <summary>
    /// Strength of quake throw
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float QuakeStrength = 4.0f;

    /// <summary>
    /// Distance, in tiles, to throw objects
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float QuakeDistance = 3.0f;

    /// <summary>
    /// Length of time to stun mobs thrown
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan QuakeStunLength = TimeSpan.FromSeconds(3);
    // Pinwheel-end - traitor sabotage
}
