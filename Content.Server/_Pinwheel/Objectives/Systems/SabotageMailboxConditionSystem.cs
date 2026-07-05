using Content.Server.Objectives.Components;
using Content.Shared.Delivery;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class SabotageMailboxConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SabotageMailboxConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<SabotageMailboxConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var enumerator = EntityQueryEnumerator<DeliverySpawnerComponent>();
        args.Progress = 0f;
        // If there's any hacked mailbox, succeed.
        while (enumerator.MoveNext(out var comp))
        {
            if (!comp.SabotageComplete)
                continue;

            args.Progress = 1f;
            return;
        }
    }
}
