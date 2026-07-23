using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio.EntitySystems; // Pinwheel - traitor sabotage
using Content.Shared.Chat; // Pinwheel - traitor sabotage
using Content.Shared.Gravity;
using Content.Shared._Pinwheel.Sabotage; // Pinwheel - traitor sabotage
using Robust.Shared.Physics.Systems; // Pinwheel - gravity drift
using Robust.Shared.Timing; // Pinwheel - gravity drift

namespace Content.Server.Gravity;

public sealed partial class GravityGeneratorSystem : SharedGravityGeneratorSystem
{
    [Dependency] private SharedChatSystem _chat = default!; // Pinwheel - traitor sabotage
    [Dependency] private GravitySystem _gravitySystem = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    // Pinwheel-stt - gravity drift
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IGameTiming _timing = default!;
    // Pinwheel-end - gravity drift
    [Dependency] private RadioSystem _radio = default!; // Pinwheel - traitor sabotage

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

    // Pinwheel-stt
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

        ent.Comp.QuakeNext = (curTime + ent.Comp.QuakeMin); // TODO: randomize this

        _chat.DispatchStationAnnouncement(ent,
            "DEBUG MESSAGE",
            sender: senderName, // TODO: de-hardcode this, somehow
            announcementSound: ent.Comp.SabotageAnnouncementSound,
            colorOverride: messageColor); // TODO: de-hardcode this too

        Log.Info($"ughhhhhhhhh - {curTime} - {ent.Comp.QuakeNext} - {ent.Comp.QuakeMin}");

        ent.Comp.QuakeWarned = false;
    }
    // Pinwheel-end

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
