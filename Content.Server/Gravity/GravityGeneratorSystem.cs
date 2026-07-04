using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Gravity;
using Robust.Shared.Physics.Systems; // Pinwheel - gravity drift
using Robust.Shared.Timing; // Pinwheel - gravity drift

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
    }

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

            // Pinwheel-stt - gravity drift
            if ((grav.NextDrift > curTime) || !grav.DriftEnabled)
                continue;

            grav.NextDrift += grav.DriftRate;

            var xform = Transform(uid);
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
            // Pinwheel-end - gravity drift
        }
    }

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
}
