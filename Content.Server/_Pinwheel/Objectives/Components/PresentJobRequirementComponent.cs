using Content.Server.Objectives.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the supplied job has at least one person present
/// </summary>
/// <remarks>
/// Excludes the objective owner from the check
/// </remarks>
[RegisterComponent, Access(typeof(PresentJobRequirementSystem))]
public sealed partial class PresentJobRequirementComponent : Component
{
    /// <summary>
    /// List of job prototype IDs to ban from having this objective.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<JobPrototype>? Job = default!;
}
