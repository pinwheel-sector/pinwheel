using Content.Shared.Audio;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.UserInterface;
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
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
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

        // player interactions
        SubscribeLocalEvent<SabotagableMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SabotagableMachineComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SabotagableMachineComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<SabotagableMachineComponent, SabotagableMachineOpenedEvent>(OnMachineOpened);
        // do-afters
        SubscribeLocalEvent<SabotagableMachineComponent, SabotageToolInsertDoAfterEvent>(OnInsertDoAfter);
        SubscribeLocalEvent<SabotagableMachineComponent, SabotageToolRemoveDoAfterEvent>(OnRemoveDoAfter);
        // external factors
        SubscribeLocalEvent<SabotagableMachineComponent, PowerChangedEvent>(OnPowerChanged);
        // progress events
        /* // these currently aren't needed for anything internally
        SubscribeLocalEvent<SabotagableMachineComponent, SabotageStartEvent>(OnSabotageStart);
        SubscribeLocalEvent<SabotagableMachineComponent, SabotageStopEvent>(OnSabotageStop);
        */
        SubscribeLocalEvent<SabotagableMachineComponent, SabotagePausedEvent>(OnSabotagePaused);
        SubscribeLocalEvent<SabotagableMachineComponent, SabotageUnPausedEvent>(OnSabotageUnPaused);
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

    private void OnInteractUsing(Entity<SabotagableMachineComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.StatusClosed)
            return; // cancel if handled or closed

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // cancel if we don't have a container

        if (!_whitelist.IsValid(ent.Comp.ToolWhitelist, args.Used))
            return; // cancel if not our sabotage tool

        var ev = new SabotageStartEvent();

        if (ent.Comp.ToolInsertTime == null)
            ProcessTool(ent, true, ev, args.User, args.Used); // if we don't have a doafter just cram it in

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

    private void OnInteractHand(Entity<SabotagableMachineComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || ent.Comp.StatusClosed)
            return; // cancel if handled or closed

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // cancel if we don't have a container

        if (container.ContainedEntities.Count < 1)
            return; // cancel if the container is empty

        var ev = new SabotageStopEvent();

        if (ent.Comp.ToolRemoveTime == null)
            ProcessTool(ent, false, ev, args.User); // if we don't have a doafter just yank it out

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

    private void OnUIOpenAttempt(Entity<SabotagableMachineComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.StatusSabotaging)
            args.Cancel();
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

        EntityEventArgs ev = default!;

        switch (args.Powered)
        {
            case true: // if we're regaining power
                ent.Comp.SabotageComplete = (curTime + ent.Comp.SabotageTimeStored); // restore our time
                ev = new SabotageUnPausedEvent();
                break;

            case false: // if we're losing power
                ent.Comp.SabotageTimeStored = (ent.Comp.SabotageComplete - curTime); // store our remaining time
                ev = new SabotagePausedEvent();
                break;
        }

        RaiseLocalEvent(ent, (object)ev);
    }

    private void OnInsertDoAfter(Entity<SabotagableMachineComponent> ent, ref SabotageToolInsertDoAfterEvent args)
    {
        if (args.Cancelled)
            return; // cancel if cancelled

        var ev = new SabotageStartEvent();

        ProcessTool(ent, true, ev, args.User, args.Used);
    }

    private void OnRemoveDoAfter(Entity<SabotagableMachineComponent> ent, ref SabotageToolRemoveDoAfterEvent args)
    {
        if (args.Cancelled)
            return; // cancel if cancelled

        var ev = new SabotageStopEvent();

        ProcessTool(ent, false, ev, args.User);
    }

    /* // see subscription
    private void OnSabotageStart(Entity<SabotagableMachineComponent> ent, ref SabotageStartEvent args)
    {
        ProcessAmbient(ent, true);
    }

    private void OnSabotageStop(Entity<SabotagableMachineComponent> ent, ref SabotageStopEvent args)
    {
        ProcessAmbient(ent, false);
    }
    */

    private void OnSabotagePaused(Entity<SabotagableMachineComponent> ent, ref SabotagePausedEvent args)
    {
        _appearance.SetData(ent, SabotagableMachineVisuals.LightState, false);
        ProcessAmbient(ent, false);
    }

    private void OnSabotageUnPaused(Entity<SabotagableMachineComponent> ent, ref SabotageUnPausedEvent args)
    {
        _appearance.SetData(ent, SabotagableMachineVisuals.LightState, true);
        ProcessAmbient(ent, true);
    }

    private void OnSabotageComplete(Entity<SabotagableMachineComponent> ent, ref SabotageCompleteEvent args)
    {
        ent.Comp.StatusSabotaging = false;
        ent.Comp.StatusSabotaged = true;

        _appearance.SetData(ent, SabotagableMachineVisuals.LightState, false);

        ProcessAmbient(ent, false);

        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.SoundComplete, ent.Owner);
    }

    private void ProcessTool(
        Entity<SabotagableMachineComponent> ent,
        bool inserting,
        EntityEventArgs raisedEvent,
        EntityUid? user = null,
        EntityUid? used = null)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ToolContainerId, out var container))
            return; // starting w/o a container shouldn't be possible but we need the ^ out var

        var sound = inserting ? ent.Comp.SoundInsert : ent.Comp.SoundRemove;

        _appearance.SetData(ent, SabotagableMachineVisuals.ToolState, inserting);
        _appearance.SetData(ent, SabotagableMachineVisuals.LightState, inserting);
        _audio.PlayPredicted(sound, ent.Owner, user);
        ProcessAmbient(ent, inserting);

        switch (inserting)
        {
            case true:
            var curTime = _timing.CurTime;
            ent.Comp.SabotageComplete = (curTime + ent.Comp.SabotageLength);
            _container.Insert(used!.Value, container);
            break;

            case false:
            foreach (var tool in container.ContainedEntities)
            {
                _hands.TryPickupAnyHand(user!.Value, tool);
            }
            break;
        }

        if (ent.Comp.StatusSabotaged)
            return; // cancel if the sabotage is complete. can't jack it twice

        ent.Comp.StatusSabotaging = inserting;
        RaiseLocalEvent(ent.Owner, (object)raisedEvent);
    }

    private void ProcessAmbient(Entity<SabotagableMachineComponent> ent, bool sabotaging)
    {
        var sound = sabotaging ? ent.Comp.SoundAmbientSabotage : ent.Comp.SoundAmbientBase;

        if (sound == null)
        {
            _ambient.SetAmbience(ent.Owner, false);
            return;
        }

        _ambient.SetAmbience(ent.Owner, true);
        _ambient.SetSound(ent.Owner, sound);
    }
}
