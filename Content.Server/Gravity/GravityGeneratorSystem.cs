using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Gravity;
// Pinwheel-stt - gravity drift
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using System.Numerics;
// Pinwheel-end - gravity drift
// Pinwheel-stt - traitor sabotage
using Content.Server.Buckle.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.Atmos.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Throwing;
using Content.Shared._Pinwheel.Sabotage;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
// Pinwheel-end - traitor sabotage

namespace Content.Server.Gravity;

public sealed partial class GravityGeneratorSystem : SharedGravityGeneratorSystem
{
    [Dependency] private GravitySystem _gravitySystem = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    // Pinwheel-stt - gravity drift
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IGameTiming _timing = default!;
    // Pinwheel-end - gravity drift
    // Pinwheel-stt - traitor sabotage
    [Dependency] private BuckleSystem _buckle = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private StunSystem _stuns = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    [Dependency] private EntityQuery<BuckleComponent> _buckleQuery = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private EntityQuery<MovedByPressureComponent> _movedByPressureQuery = default!;
    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;
    // Pinwheel-end - traitor sabotage

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
        // Pinwheel-stt - traitor sabotage
        SubscribeLocalEvent<GravityGeneratorComponent, SabotagableMachineOpenedEvent>(OnMachineOpened);
        SubscribeLocalEvent<GravityGeneratorComponent, SabotageStartEvent>(OnSabotageStart);
        SubscribeLocalEvent<GravityGeneratorComponent, SabotageStopEvent>(OnSabotageStop);
        SubscribeLocalEvent<GravityGeneratorComponent, SabotageCompleteEvent>(OnSabotageComplete);
        // Pinwheel-end - traitor sabotage
    }

    // Pinwheel-stt - traitor sabotage
    // TODO: this shit should either be interpreted or in the component
    private Color messageColor = new Color(255, 115, 60); // engineering radio color
    private string senderName = "Gravity Generator";
    // Pinwheel-end - traitor sabotage

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime; // Pinwheel - gravity drift

        var query = EntityQueryEnumerator<GravityGeneratorComponent, PowerChargeComponent>();
        while (query.MoveNext(out var uid, out var grav, out var charge))
        {
            if (!_lights.TryGetLight(uid, out var pointLight))
                continue;

            _lights.SetEnabled(uid, charge.Charge > 0, pointLight);
            _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, charge.Charge),
                pointLight);

            // Pinwheel-stt
            if ((grav.DriftNext < curTime) && grav.DriftEnabled)
                HandleDrift((uid, grav));

            if ((grav.QuakeNext < (curTime + grav.QuakeWarning)) && grav.SabotageComplete)
                HandleQuakeWarning((uid, grav));

            if ((grav.QuakeNext < curTime) && grav.SabotageComplete)
                HandleQuake((uid, grav));
            // Pinwheel-end
        }
    }

    // Pinwheel-stt - gravity drift
    private void HandleDrift(Entity<GravityGeneratorComponent> ent)
    {
        ent.Comp.DriftNext += ent.Comp.DriftRate;

        var xform = Transform(ent.Owner);
        var worldPos = _transform.GetWorldPosition(xform);

        // get all entities with GravityDrift
        var drifters = EntityQueryEnumerator<GravityDriftComponent, TransformComponent>();
        while (drifters.MoveNext(out var driftUid, out var drift, out var driftXform))
        {
            // reset the strength and skip to the next entity if grounded
            if (driftXform.GridUid != null)
                {
                    drift.DriftStrength = 0;
                    continue;
                }

            var dir = (_transform.GetWorldPosition(driftXform) - worldPos).Normalized();

            if (drift.DriftStrength < drift.DriftMax)
                drift.DriftStrength += drift.DriftAdd;

            _physics.ApplyLinearImpulse(driftUid, (-dir * drift.DriftStrength));
        }
    }
    // Pinwheel-end - gravity drift

    // Pinwheel-stt - traitor sabotage
    private void HandleQuakeWarning(Entity<GravityGeneratorComponent> ent)
    {
        if (ent.Comp.QuakeWarned)
            return; // don't spam the announcements

        string message = Loc.GetString(ent.Comp.MessageQuake);
        _chat.DispatchStationAnnouncement(ent,
            message,
            sender: senderName, // TODO: de-hardcode this, somehow
            announcementSound: ent.Comp.SabotageAnnouncementSound,
            colorOverride: messageColor); // TODO: de-hardcode this too

        ent.Comp.QuakeWarned = true;
    }

    private void HandleQuake(Entity<GravityGeneratorComponent> ent)
    {
        var curTime = _timing.CurTime;
        var xform = Transform(ent);

        ent.Comp.QuakeNext = (curTime + ent.Comp.QuakeMin); // TODO: randomize this

        ThrowEntitiesOnGrid(xform.ParentUid, ent);

        ent.Comp.QuakeWarned = false;
    }

    private static bool GridQueryCallback(
        ref (List<Entity<PhysicsComponent>> List, HashSet<EntityUid> Processed, EntityQuery<PhysicsComponent> PhysicsQuery) state,
        in EntityUid uid)
    {
        if (state.Processed.Add(uid) && state.PhysicsQuery.TryComp(uid, out var body))
            state.List.Add((uid, body));

        return true;
    }

    private static bool GridQueryCallback(
        ref (List<Entity<PhysicsComponent>> List, HashSet<EntityUid> Processed, EntityQuery<PhysicsComponent> PhysicsQuery) state,
        in FixtureProxy proxy)
    {
        var owner = proxy.Entity;
        return GridQueryCallback(ref state, in owner);
    }

    private void ThrowEntitiesOnGrid(EntityUid gridUid, Entity<GravityGeneratorComponent> generator)
    { // ripped in large part from ShuttleSystem.Impact.cs

        var xform = Transform(generator);

        // iterate all dynamic entities on the grid
        if (!TryComp<BroadphaseComponent>(gridUid, out var lookup) || !_gridQuery.TryComp(gridUid, out var gridComp))
            return;

        var gridBox = gridComp.LocalAABB;
        List<Entity<PhysicsComponent>> list = new();
        HashSet<EntityUid> processed = new();
        var state = (list, processed, _physicsQuery);
        lookup.DynamicTree.QueryAabb(ref state, GridQueryCallback, gridBox, true);
        lookup.SundriesTree.QueryAabb(ref state, GridQueryCallback, gridBox, true);

        foreach (var ent in list)
        {
            // don't throw if buckled
            if (_buckle.IsBuckled(ent, _buckleQuery.CompOrNull(ent)))
                continue;

            // don't throw them if they have magboots
            if (_movedByPressureQuery.TryComp(ent, out var moved) && !moved.Enabled)
                continue;

            var dir = (_transform.GetWorldPosition(ent) - _transform.GetWorldPosition(xform)).Normalized();

            _stuns.TryCrawling(ent.Owner, generator.Comp.QuakeStunLength);
            _throwing.TryThrow(
                uid: ent.Owner,
                direction: (-dir * generator.Comp.QuakeDistance),
                baseThrowSpeed: (dir.Length() * generator.Comp.QuakeStrength),
                compensateFriction: true,
                doSpin: true);
        }
    }
    // Pinwheel-end - traitor sabotage

    private void OnActivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineActivatedEvent args)
    {
        ent.Comp.GravityActive = true;
        Dirty(ent, ent.Comp);

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.EnableGravity(xform.ParentUid, gravity);
        }
    }

    private void OnDeactivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        ent.Comp.GravityActive = false;
        Dirty(ent, ent.Comp);

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
        }
    }

    private void OnParentChanged(EntityUid uid, GravityGeneratorComponent component, ref EntParentChangedMessage args)
    {
        if (component.GravityActive && TryComp(args.OldParent, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(args.OldParent.Value, gravity);
        }
    }

// Pinwheel-stt - traitor sabotage
    private void OnMachineOpened(Entity<GravityGeneratorComponent> ent, ref SabotagableMachineOpenedEvent args)
    {
        string message = Loc.GetString(ent.Comp.MessageOpen);
        _radio.SendRadioMessage(ent, message, ent.Comp.MessageChannel, ent);
    }

    private void OnSabotageStart(Entity<GravityGeneratorComponent> ent, ref SabotageStartEvent args)
    {
        string message = Loc.GetString(ent.Comp.MessageStart);
        _chat.DispatchStationAnnouncement(ent,
            message,
            sender: senderName, // TODO: de-hardcode this, somehow
            announcementSound: ent.Comp.SabotageAnnouncementSound,
            colorOverride: messageColor); // TODO: de-hardcode this too
    }

    private void OnSabotageStop(Entity<GravityGeneratorComponent> ent, ref SabotageStopEvent args)
    {
        string message = Loc.GetString(ent.Comp.MessageStop);
        _chat.DispatchStationAnnouncement(ent,
            message,
            sender: senderName, // TODO: de-hardcode this, somehow
            announcementSound: ent.Comp.SabotageAnnouncementSound,
            colorOverride: messageColor); // TODO: de-hardcode this too
    }

    private void OnSabotageComplete(Entity<GravityGeneratorComponent> ent, ref SabotageCompleteEvent args)
    {
        var curTime = _timing.CurTime;

        ent.Comp.QuakeNext = (curTime + ent.Comp.QuakeMin); // TODO: randomize this

        ent.Comp.SabotageComplete = true;

        string message = Loc.GetString(ent.Comp.MessageComplete);
        _chat.DispatchStationAnnouncement(ent,
            message,
            sender: senderName, // TODO: de-hardcode this, somehow
            announcementSound: ent.Comp.SabotageAnnouncementSound,
            colorOverride: messageColor); // TODO: de-hardcode this too
    }
// Pinwheel-end - traitor sabotage
}
