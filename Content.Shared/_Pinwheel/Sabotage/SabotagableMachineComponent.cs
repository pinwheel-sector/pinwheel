using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared._Pinwheel.Sabotage;

/// <summary>
/// Generic component handling sabotage tool insertion and function.
/// Event based, the consequences are handled in a bespoke manner on target systems.
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(SabotagableMachineSystem))]
public sealed partial class SabotagableMachineComponent : Component
{
    /// <summary>
    /// Length of time the sabotage will take
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SabotageLength = TimeSpan.FromSeconds(120);

    /// <summary>
    /// When the sabotage will complete
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan SabotageComplete = new();

    /// <summary>
    /// Is the sabotage currently in progress
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Sabotaging = false;

    /// <summary>
    /// Is the sabotage complete
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Sabotaged = false;

    /// <summary>
    /// Length of doafter for inserting the sabotage tool
    /// </summary>
    /// <remarks>
    /// If null the interaction completes instantly
    /// </remarks>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? ToolInsertTime = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Length of doafter for removing the sabotage tool
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? ToolRemoveTime = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Whitelist for what counts as this machine's bespoke sabotage tool
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityWhitelist ToolWhitelist = new();

    /// <summary>
    /// Sound to play on complete insertion
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? InsertSound = default!;

    /// <summary>
    /// Sound to play on complete removal
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? RemoveSound = default!;

    /// <summary>
    /// Container for the sabotage tool
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string ToolContainerId = "sabotage_container";

    /// <summary>
    /// Is the container obstructed by a lock or panel or somesuch
    /// </summary>
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Closed = true;
}
