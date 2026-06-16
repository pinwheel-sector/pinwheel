using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// Marker component used to grab the entity directly instead of it's contents
/// </summary>
/// <remarks>
/// Without NetworkedComponentAttribute this mispredicts on the client
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoSmartEquipRecurseComponent : Component;
