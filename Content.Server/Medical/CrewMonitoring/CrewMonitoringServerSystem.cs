using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.Radio.EntitySystems; // Pinwheel - traitor sabotage
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power.EntitySystems; // Pinwheel - traitor sabotage
using Content.Shared._Pinwheel.Sabotage; // Pinwheel - traitor sabotage
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Server.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private SharedPowerReceiverSystem _power = default!; // Pinwheel - traitor sabotage
    [Dependency] private RadioSystem _radio = default!; // Pinwheel - traitor sabotage
    [Dependency] private SuitSensorSystem _sensors = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private SingletonDeviceNetServerSystem _singletonServerSystem = default!;

    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetServerDisconnectedEvent>(OnDisconnected);
        // Pinwheel-stt - traitor sabotage
        SubscribeLocalEvent<CrewMonitoringServerComponent, SabotagableMachineOpenedEvent>(OnMachineOpened);
        SubscribeLocalEvent<CrewMonitoringServerComponent, SabotageStopEvent>(OnSabotageStop); // technically true
        // Pinwheel-end - traitor sabotage
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var servers = EntityQueryEnumerator<CrewMonitoringServerComponent>();

        while (servers.MoveNext(out var id, out var server))
        {
            if (!_singletonServerSystem.IsActiveServer(id))
                continue;

            UpdateTimeout(id);
            BroadcastSensorStatus(id, server);
        }
    }

    /// <summary>
    /// Adds or updates a sensor status entry if the received package is a sensor status update
    /// </summary>
    private void OnPacketReceived(EntityUid uid, CrewMonitoringServerComponent component, DeviceNetworkPacketEvent args)
    {
        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        // Pinwheel-stt - traitor sabotage
        if (component.Sabotaged)
        {
            sensorStatus.TotalDamage = null;
            sensorStatus.TotalDamageThreshold = null;
        }
        // Pinwheel-end - traitor sabotage

        sensorStatus.Timestamp = _gameTiming.CurTime;
        component.SensorStatus[args.SenderAddress] = sensorStatus;
    }

    /// <summary>
    /// Clears the servers sensor status list
    /// </summary>
    private void OnRemove(EntityUid uid, CrewMonitoringServerComponent component, ComponentRemove args)
    {
        component.SensorStatus.Clear();
    }

    /// <summary>
    /// Drop the sensor status if it hasn't been updated for to long
    /// </summary>
    private void UpdateTimeout(EntityUid uid, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        foreach (var (address, sensor) in component.SensorStatus)
        {
            var dif = _gameTiming.CurTime - sensor.Timestamp;
            if (dif.Seconds > component.SensorTimeout)
                component.SensorStatus.Remove(address);
        }
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors
    /// </summary>
    private void BroadcastSensorStatus(EntityUid uid, CrewMonitoringServerComponent? serverComponent = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref serverComponent, ref device))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = serverComponent.SensorStatus
        };

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
    }

    /// <summary>
    /// Clears sensor data on disconnect
    /// </summary>
    private void OnDisconnected(EntityUid uid, CrewMonitoringServerComponent component, ref DeviceNetServerDisconnectedEvent _)
    {
        component.SensorStatus.Clear();
    }

    // Pinwheel-stt - traitor sabotage
    private void OnMachineOpened(Entity<CrewMonitoringServerComponent> ent, ref SabotagableMachineOpenedEvent args)
    {
        if (!_power.IsPowered(ent.Owner))
            return; // cancel if unpowered

        string message = Loc.GetString(ent.Comp.MessageOpen);
        _radio.SendRadioMessage(ent, message, ent.Comp.MessageChannel, ent);
    }

    private void OnSabotageStop(Entity<CrewMonitoringServerComponent> ent, ref SabotageStopEvent args)
    {
        ent.Comp.Sabotaged = true;

        if (!_power.IsPowered(ent.Owner))
            return; // cancel if unpowered

        string message = Loc.GetString(ent.Comp.MessageRemove);
        _radio.SendRadioMessage(ent, message, ent.Comp.MessageChannel, ent);
    }
    // Pinwheel-end - traitor sabotage
}
