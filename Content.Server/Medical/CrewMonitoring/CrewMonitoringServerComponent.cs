using Content.Shared.Radio; // Pinwheel - traitor sabotage
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Map;
using Robust.Shared.Prototypes; // Pinwheel - traitor sabotage

namespace Content.Server.Medical.CrewMonitoring;

[RegisterComponent]
[Access(typeof(CrewMonitoringServerSystem))]
public sealed partial class CrewMonitoringServerComponent : Component
{

    /// <summary>
    ///     List of all currently connected sensors to this server.
    /// </summary>
    public readonly Dictionary<string, SuitSensorStatus> SensorStatus = new();

    /// <summary>
    ///     After what time sensor consider to be lost.
    /// </summary>
    [DataField("sensorTimeout"), ViewVariables(VVAccess.ReadWrite)]
    public float SensorTimeout = 10f;

    // Pinwheel-stt - traitor sabotage
    /// <summary>
    /// Has this server been sabotaged
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool Sabotaged = false;

    /// <summary>
    /// The radio channel to whine on when something goes wrong
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> MessageChannel = "Medical";

    /// <summary>
    /// Message to send when the server's shell is removed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public LocId MessageDamage = "server-damage-message";

    /// <summary>
    /// Message to use when the disk is removed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public LocId MessageSabotage = "server-sabotage-message";
    // Pinwheel-end - traitor sabotage
}
