using Content.Shared.Anomaly;
using Content.Shared.Materials;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
    [DataField("cooldownEndTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan CooldownEndTime = TimeSpan.Zero;

    /// <summary>
    /// The cooldown between generating anomalies.
    /// </summary>
    [DataField("cooldownLength"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan CooldownLength = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long it takes to generate an anomaly after pushing the button.
    /// </summary>
    [DataField("generationLength"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GenerationLength = TimeSpan.FromSeconds(8);

    /// <summary>
    /// The material needed to generate an anomaly
    /// </summary>
    [DataField("requiredMaterial", customTypeSerializer: typeof(PrototypeIdSerializer<MaterialPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string RequiredMaterial = "Plasma";

    /// <summary>
    /// The amount of material needed to generate a single anomaly
    /// </summary>
    [DataField("materialPerAnomaly"), ViewVariables(VVAccess.ReadWrite)]
    public int MaterialPerAnomaly = 1500; // a bit less than a stack of plasma

    /// <summary>
    /// The random anomaly spawner entity
    /// </summary>
    [DataField("spawnerPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string SpawnerPrototype = "RandomAnomalySpawner";

    /// <summary>
    /// The radio channel for science
    /// </summary>
    [DataField("scienceChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string ScienceChannel = "Science";

    /// <summary>
    /// The sound looped while an anomaly generates
    /// </summary>
    [DataField("generatingSound")]
    public SoundSpecifier? GeneratingSound;

    /// <summary>
    /// Sound played on generation completion.
    /// </summary>
    [DataField("generatingFinishedSound")]
    public SoundSpecifier? GeneratingFinishedSound;

    // Pinwheel-stt - traitor sabotage
    /// <summary>
    /// Message to use when the maintenance panel is taken off
    /// </summary>
    [DataField]
    public LocId MessageInsert = "sabotage-message-start-anomaly";

    /// <summary>
    /// Message to use when the sabotage doohickey is removed
    /// </summary>
    [DataField]
    public LocId MessageRemove = "sabotage-message-stop-anomaly";

    /// <summary>
    /// Sound played with sabotage announcement
    /// </summary>
    [DataField]
    public SoundSpecifier? SabotageAnnouncementSound;

    /// <summary>
    /// Message to use when the sabotage macguffin does its thing
    /// </summary>
    [DataField]
    public LocId MessageComplete = "sabotage-message-complete-anomaly";

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
