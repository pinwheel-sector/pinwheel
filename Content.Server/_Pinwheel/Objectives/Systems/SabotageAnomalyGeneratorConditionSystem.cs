using Content.Server.Anomaly.Components;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <remarks>
/// TODO: This and <see cref="SabotageMailboxConditionSystem"/> et alii need to be consolidated
/// </remarks>
public sealed class SabotageAnomalyGeneratorConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SabotageAnomalyGeneratorConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<SabotageAnomalyGeneratorConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var enumerator = EntityQueryEnumerator<AnomalyGeneratorComponent>();
        args.Progress = 0f;
        // If there's any hacked anomaly generator, succeed.
        while (enumerator.MoveNext(out var comp))
        {
            if (comp.SabotageComplete)
                args.Progress = 1f;
        }
    }
}
