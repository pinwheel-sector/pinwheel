using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Stealth.Components;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Pinwheel.Whistle;

/// <summary>
/// On action or use, plays a sound and spawns an entity attached to all entities with <see cref="HumanoidAppearanceComponent"/> in range.
/// </summary>
public sealed partial class WhistleSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WhistleComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<WhistleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<WhistleComponent, WhistleActionEvent>(OnWhistleAction);
    }

    private void OnGetActions(Entity<WhistleComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
    }

    public void OnWhistleAction(Entity<WhistleComponent> ent, ref WhistleActionEvent args)
    {
        if (args.Handled || !_timing.IsFirstTimePredicted)
            return;

        MakeLoudWhistle(ent, args.Performer);
        args.Handled = true;
    }

    public void OnUseInHand(Entity<WhistleComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !_timing.IsFirstTimePredicted)
            return;

        MakeLoudWhistle(ent, args.User);

        args.Handled = true;
    }

    private void MakeLoudWhistle(Entity<WhistleComponent> ent, EntityUid user)
    {
        StealthComponent? stealth = null;

        if (TryComp<UseDelayComponent>(ent, out var useDelay))
        {
            _actions.SetCooldown(ent.Comp.Action, useDelay.Delay);
            _useDelay.SetLength(user, useDelay.Delay);
            _useDelay.TryResetDelay((user, useDelay));
        }

        _audio.PlayPredicted(ent.Comp.WhistleSound, ent.Owner, user);

        foreach (var iterator in
            _entityLookup.GetEntitiesInRange<HumanoidProfileComponent>(_transform.GetMapCoordinates(ent),
            ent.Comp.Distance))
        {
            //Avoid pinging invisible entities
            if (TryComp(iterator, out stealth) && stealth.Enabled)
                continue;

            //We don't want to ping user of whistle
            if (iterator.Owner == user)
                continue;

            SpawnAttachedTo(ent.Comp.Effect, iterator.Owner.ToCoordinates());
        }
    }
}
