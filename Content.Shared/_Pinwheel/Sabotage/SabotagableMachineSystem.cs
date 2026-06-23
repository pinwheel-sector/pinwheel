using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._Pinwheel.Sabotage;

/// <summary>
/// Generic system handling sabotage tool insertion and function.
/// Event based, the consequences are handled in a bespoke manner on target systems.
/// </summary>
public sealed partial class SabotagableMachineSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        // inserting the tool
        SubscribeLocalEvent<SabotagableMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SabotagableMachineComponent, SabotageToolInsertDoAfterEvent>(OnInsertDoAfter);
        // removing the tool
        SubscribeLocalEvent<SabotagableMachineComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SabotagableMachineComponent, SabotageToolRemoveDoAfterEvent>(OnRemoveDoAfter);
    }

    private void OnInteractUsing(Entity<SabotagableMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return; // cancel if handled

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // cancel if we don't have a container

        if (!_whitelist.IsValid(ent.Comp.ToolWhitelist, args.Used))
            return; // cancel if not our sabotage tool

        var ev = new SabotageToolInsertEvent(args.User, args.Used, ent.Owner);

        if (ent.Comp.ToolInsertTime == null)
        { // if we don't have a doafter just cram it in
            _container.Insert(args.Used, container);
            _audio.PlayPredicted(ent.Comp.InsertSound, ent.Owner, args.User);
            RaiseLocalEvent(ent.Owner, ev);
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.ToolInsertTime!.Value,
            new SabotageToolInsertDoAfterEvent(),
            ent.Owner,
            used: args.Used)
            {
                BreakOnHandChange = true,
                BreakOnDropItem = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                NeedHand = true,
                RequireCanInteract = true
            };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnInsertDoAfter(Entity<SabotagableMachineComponent> ent, ref SabotageToolInsertDoAfterEvent args)
    {
        if (args.Cancelled)
            return; // cancel if cancelled

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // doafter can't start w/o a container but we need the out var

        var ev = new SabotageToolInsertEvent(args.User, args.Used!.Value, ent.Owner);

        _container.Insert(args.Used!.Value, container);
        _audio.PlayPredicted(ent.Comp.InsertSound, ent.Owner, args.User);
        RaiseLocalEvent(ent.Owner, ev);
    }

    private void OnInteractHand(Entity<SabotagableMachineComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return; // cancel if handled

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // cancel if we don't have a container

        if (container.ContainedEntities.Count < 1)
            return; // cancel if the container is empty

        var ev = new SabotageToolRemoveEvent(args.User, ent.Owner);

        if (ent.Comp.ToolRemoveTime == null)
        { // if we don't have a doafter just yank it out
            foreach (var tool in container.ContainedEntities)
            {
                _hands.TryPickupAnyHand(args.User, tool);
            }

            _audio.PlayPredicted(ent.Comp.RemoveSound, ent.Owner, args.User);
            RaiseLocalEvent(ent.Owner, ev);
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.ToolRemoveTime!.Value,
            new SabotageToolRemoveDoAfterEvent(),
            ent.Owner)
            {
                BreakOnHandChange = true,
                BreakOnDropItem = true,
                BreakOnMove = true,
                BreakOnWeightlessMove = true,
                NeedHand = true,
                RequireCanInteract = true
            };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnRemoveDoAfter(Entity<SabotagableMachineComponent> ent, ref SabotageToolRemoveDoAfterEvent args)
    {
        if (args.Cancelled)
            return; // cancel if cancelled

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // doafter can't start w/o a container but we need the out var

        var ev = new SabotageToolRemoveEvent(args.User, ent.Owner);

        foreach (var tool in container.ContainedEntities)
        {
            _hands.TryPickupAnyHand(args.User, tool);
        }

        _audio.PlayPredicted(ent.Comp.RemoveSound, ent.Owner, args.User);
        RaiseLocalEvent(ent.Owner, ev);
    }
}
