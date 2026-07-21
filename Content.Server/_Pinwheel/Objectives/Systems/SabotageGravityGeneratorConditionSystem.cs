using Content.Server.Objectives.Components;
using Content.Shared.Gravity;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <remarks>
/// TODO: This and <see cref="SabotageMailboxConditionSystem"/> et alii need to be consolidated
/// </remarks>
public sealed class SabotageGravityGeneratorConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SabotageGravityGeneratorConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<SabotageGravityGeneratorConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var enumerator = EntityQueryEnumerator<GravityGeneratorComponent>();
        args.Progress = 0f;
        // If there's any hacked gravity generator, succeed.
        while (enumerator.MoveNext(out var comp))
        {
            if (comp.SabotageComplete)
                args.Progress = 1f;
        }
    }
}
