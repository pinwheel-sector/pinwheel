using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Requires that the supplied jobs have at least one person present
/// </summary>
public sealed partial class PresentJobRequirementSystem : EntitySystem
{
    [Dependency] private SharedJobSystem _jobs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PresentJobRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(Entity<PresentJobRequirementComponent> ent, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        bool failed = true;

        var allJobs = EntityQueryEnumerator<MindComponent>();
        while (allJobs.MoveNext(out var uid, out var mind))
        {
            if (uid == args.MindId)
                continue;

            if (_jobs.MindTryGetJobId(uid, out var proto) && (proto == ent.Comp.Job))
                failed = false;
        }

        args.Cancelled = failed;
    }
}
