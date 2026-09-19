using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared._Pinwheel.AlienRock;
using Robust.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared._Pinwheel.AlienRock.Equipment;

/// <summary>
/// Handheld tool displaying the nodes present on an artifact
/// </summary>
public sealed partial class AlienScannerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Update(float frameTime)
    {
        var scannerQuery = EntityQueryEnumerator<AlienScannerConnectedComponent>();
        while (scannerQuery.MoveNext(out var uid, out var scan))
        {
            if (scan.UpdateNext > _timing.CurTime)
                continue;

            if (!TryComp<AlienRockScannedComponent>(scan.Attached, out var rock))
                continue;

            scan.UpdateNext = _timing.CurTime + scan.UpdateRate;

            var xform1 = Transform(uid);
            var xform2 = Transform(scan.Attached);
            if (!_transform.InRange(xform1.Coordinates, xform2.Coordinates, scan.Range))
            {
                RemCompDeferred(uid, scan);
                RemCompDeferred(scan.Attached, rock);
            }
        }
    }

    private void Attach(
        Entity<AlienScannerComponent> ent,
        Entity<AlienRockComponent?> rock,
        EntityUid actor)
    {
        if (!Resolve(rock.Owner, ref rock.Comp))
            throw new Exception($"Entity {rock.Owner} has no {typeof(AlienRockComponent)}. How did you even scan it");

        var scanner = EnsureComp<AlienScannerConnectedComponent>(ent);
        if (scanner.Attached != rock.Owner)
        {
            scanner.Attached = rock.Owner;
            Dirty(ent, scanner);
        }

        var scanned = EnsureComp<AlienRockScannedComponent>(rock);
        if (scanned.Attached != ent.Owner)
        {
            scanned.Attached = ent.Owner;
            Dirty(rock, scanned);
        }

        _ui.TryOpenUi((ent, null), AlienScannerUiKey.Key, actor, predicted: true);
    }

    [SubscribeLocalEvent]
    private void OnScannerBeforeRangedInteract(
        Entity<AlienScannerComponent> ent,
        ref BeforeRangedInteractEvent args)
    {
        if (args.Handled
            || !args.CanReach
            || args.Target is not { } target
            || !TryComp<AlienRockComponent>(target, out var rock))
            return;

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.DoAfterLength,
            new AlienScannerDoAfterEvent(),
            ent.Owner,
            used: args.Used,
            target: args.Target)
            {
                BreakOnHandChange = false,
                BreakOnDropItem = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                NeedHand = true,
                RequireCanInteract = true
            };

        _doAfter.TryStartDoAfter(doAfter);

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnScannerDoAfter(
        Entity<AlienScannerComponent> ent,
        ref AlienScannerDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is null)
            return;

        Attach(ent, args.Target.Value, args.User);
    }

}

[Serializable, NetSerializable]
public sealed partial class AlienScannerDoAfterEvent : SimpleDoAfterEvent;
