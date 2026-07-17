using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Pinwheel.Sabotage;

/// <summary>
/// Raised when the sabotage tool is finished inserting
/// </summary>
public sealed class SabotageStartEvent : EntityEventArgs;

/// <summary>
/// Raised when the sabotage tool is finished being removed
/// </summary>
public sealed class SabotageStopEvent : EntityEventArgs;

/// <summary>
/// Raised when the machine loses power
/// </summary>
public sealed class SabotagePausedEvent : EntityEventArgs;

/// <summary>
/// Raised when the machine regains power
/// </summary>
public sealed class SabotageUnPausedEvent : EntityEventArgs;

/// <summary>
/// Raised when the sabotage process is complete
/// </summary>
public sealed class SabotageCompleteEvent : EntityEventArgs;

/// <summary>
/// Raised by construction graphs to indicate the tool container should open
/// </summary>
[DataDefinition]
public sealed partial class SabotagableMachineOpenedEvent : EntityEventArgs;

/// <summary>
/// Used for the tool insertion doafter
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SabotageToolInsertDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Used for the tool removal doafter
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SabotageToolRemoveDoAfterEvent : SimpleDoAfterEvent;
