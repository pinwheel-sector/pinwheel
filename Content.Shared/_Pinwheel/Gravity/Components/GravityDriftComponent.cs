namespace Content.Shared.Gravity;

/// <summary>
/// Marker component used by <see cref=GravityGeneratorSystem/>
/// to drift entities in the direction of the station.
/// </summary>
[RegisterComponent]
public sealed partial class GravityDriftComponent : Component
{
    /// <summary>
    /// Force to apply to drifting entities in the direction of the generator
    /// Reset to 0 when it's parented to a grid
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float DriftStrength = 0f;

    /// <summary>
    /// Maximum force the gravity drift should reach
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float DriftMax = 10f;

    /// <summary>
    /// How much force to accumulate per drift impulse
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float DriftAdd = 0.2f;
}
