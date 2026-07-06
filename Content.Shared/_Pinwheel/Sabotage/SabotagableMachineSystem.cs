using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Pinwheel.Sabotage;

/// <summary>
/// Generic system handling sabotage tool insertion and function.
/// Event based, the consequences are handled in a bespoke manner on target systems.
/// </summary>
public sealed partial class SabotagableMachineSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private IGameTiming _timing = default!;
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
        // sabotage process
        SubscribeLocalEvent<SabotagableMachineComponent, SabotagableMachineOpenedEvent>(OnMachineOpened);
        SubscribeLocalEvent<SabotagableMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SabotagableMachineComponent, SabotageCompleteEvent>(OnSabotageComplete);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<SabotagableMachineComponent>();
        while (query.MoveNext(out var uid, out var machine))
        {
            if (machine.StatusSabotaged || !_power.IsPowered(uid))
                continue; // skip if we're already sabotaged or we're missing power

            if ((curTime < machine.SabotageComplete) || (!machine.StatusSabotaging))
                continue; // skip if we're not being sabotaged or it's not done yet

            var ev = new SabotageCompleteEvent();

            RaiseLocalEvent(uid, ev);
        }
    }

    private void ProcessTool(Entity<SabotagableMachineComponent> ent, bool inserting, EntityEventArgs raisedEvent, EntityUid? user)
    { // helper function to handle tool moving feedback
        var sound = inserting ? ent.Comp.SoundInsert : ent.Comp.SoundRemove;

        _appearance.SetData(ent, SabotagableMachineVisuals.ToolState, inserting);
        _audio.PlayPredicted(sound, ent.Owner, user);

        if (ent.Comp.StatusSabotaged)
            return; // cancel if the sabotage is complete. can't jack it twice

        ent.Comp.StatusSabotaging = inserting;
        RaiseLocalEvent(ent.Owner, (object)raisedEvent);

        if (inserting)
        {
            var curTime = _timing.CurTime;
            ent.Comp.SabotageComplete = (curTime + ent.Comp.SabotageLength);
        }
    }

    private void OnInteractUsing(Entity<SabotagableMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.StatusClosed)
            return; // cancel if handled or closed

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // cancel if we don't have a container

        if (!_whitelist.IsValid(ent.Comp.ToolWhitelist, args.Used))
            return; // cancel if not our sabotage tool

        var ev = new SabotageToolInsertEvent(args.User, args.Used);

        if (ent.Comp.ToolInsertTime == null)
        { // if we don't have a doafter just cram it in
            _container.Insert(args.Used, container);
            ProcessTool(ent, true, ev, args.User);
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

        var ev = new SabotageToolInsertEvent(args.User, args.Used!.Value);

        _container.Insert(args.Used!.Value, container);
        ProcessTool(ent, true, ev, args.User);
    }

    private void OnInteractHand(Entity<SabotagableMachineComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || ent.Comp.StatusClosed)
            return; // cancel if handled or closed

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // cancel if we don't have a container

        if (container.ContainedEntities.Count < 1)
            return; // cancel if the container is empty

        var ev = new SabotageToolRemoveEvent(args.User);

        if (ent.Comp.ToolRemoveTime == null)
        { // if we don't have a doafter just yank it out
            foreach (var tool in container.ContainedEntities)
            {
                _hands.TryPickupAnyHand(args.User, tool);
            }

            ProcessTool(ent, false, ev, args.User);
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

        var ev = new SabotageToolRemoveEvent(args.User);

        foreach (var tool in container.ContainedEntities)
        {
            _hands.TryPickupAnyHand(args.User, tool);
        }

        ProcessTool(ent, false, ev, args.User);
    }

    private void OnMachineOpened(Entity<SabotagableMachineComponent> ent, ref SabotagableMachineOpenedEvent args)
    {
        ent.Comp.StatusClosed = false;
    }

    private void OnPowerChanged(Entity<SabotagableMachineComponent> ent, ref PowerChangedEvent args)
    {
        if (!ent.Comp.StatusSabotaging)
            return; // cancel if nothing is happening

        var curTime = _timing.CurTime;

        if (!args.Powered)
        { // if we're losing power
            ent.Comp.SabotageTimeStored = (ent.Comp.SabotageComplete - curTime); // store our remaining time
            return; // we did our job
        }

        // if we're regaining power
        ent.Comp.SabotageComplete = (curTime + ent.Comp.SabotageTimeStored); // restore our time
    }

    private void OnSabotageComplete(Entity<SabotagableMachineComponent> ent, ref SabotageCompleteEvent args)
    {
        ent.Comp.StatusSabotaging = false;
        ent.Comp.StatusSabotaged = true;
        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.SoundComplete, ent.Owner);
    }
}
