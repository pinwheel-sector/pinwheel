using Content.Shared.Anomaly;
using Content.Shared.Materials;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This is used for a machine that is able to generate
/// anomalies randomly on the station.
/// </summary>
[RegisterComponent, Access(typeof(SharedAnomalySystem)), AutoGenerateComponentPause]
public sealed partial class AnomalyGeneratorComponent : Component
{
    /// <summary>
    /// The time at which the cooldown for generating another anomaly will be over
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan CooldownEndTime = TimeSpan.Zero;

    /// <summary>
    /// The cooldown between generating anomalies.
    /// </summary>
    [DataField]
    public TimeSpan CooldownLength = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long it takes to generate an anomaly after pushing the button.
    /// </summary>
    [DataField]
    public TimeSpan GenerationLength = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The material needed to generate an anomaly
    /// </summary>
    [DataField]
    public ProtoId<MaterialPrototype> RequiredMaterial = "Plasma";

    /// <summary>
    /// The amount of material needed to generate a single anomaly
    /// </summary>
    [DataField]
    public int MaterialPerAnomaly = 1500; // half a stack of plasma

    /// <summary>
    /// The random anomaly spawner entity
    /// </summary>
    [DataField]
    public EntProtoId SpawnerPrototype = "RandomAnomalySpawner";

    /// <summary>
    /// The radio channel for science
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> ScienceChannel = "Science";

    /// <summary>
    /// The sound looped while an anomaly generates
    /// </summary>
    [DataField]
    public SoundSpecifier? GeneratingSound;

    /// <summary>
    /// Sound played on generation completion.
    /// </summary>
    [DataField]
    public SoundSpecifier? GeneratingFinishedSound;

    // Pinwheel-stt - traitor sabotage
    /// <summary>
    /// Message to use when the sabotage doodad is jammed in
    /// </summary>
    [DataField]
    public LocId MessageInsert = "sabotage-message-start-anomaly";

    /// <summary>
    /// Message to use when the sabotage doohickey is removed
    /// </summary>
    [DataField]
    public LocId MessageRemove = "sabotage-message-stop-anomaly";

    /// <summary>
    /// Message to use when the sabotage macguffin does its thing
    /// </summary>
    [DataField]
    public LocId MessageComplete = "sabotage-message-complete-anomaly";

    /// <summary>
    /// Sound played with sabotage announcement
    /// </summary>
    [DataField]
    public SoundSpecifier? SabotageAnnouncementSound;

    /// <summary>
    /// Anomalies to spawn on complete sabotage
    /// </summary>
    [DataField]
    public int SabotageAnomalyCount = 4;

    /// <summary>
    /// Is the sabotage complete. Checked by the objective
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SabotageComplete = false;
    // Pinwheel-end - traitor sabotage
}
