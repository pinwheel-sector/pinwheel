using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Pinwheel.Sabotage;

/// <summary>
/// Raised when the sabotage tool is finished inserting
/// </summary>
public sealed class SabotageToolInsertEvent(EntityUid user, EntityUid used, EntityUid target) : EntityEventArgs
{
    /// <summary>
    /// The entity inserting the tool
    /// </summary>
    public readonly EntityUid User = user;

    /// <summary>
    /// The sabotage tool being inserted
    /// </summary>
    public readonly EntityUid Used = used;

    /// <summary>
    /// The entity getting sabotaged
    /// </summary>
    public readonly EntityUid Target = target;
}

/// <summary>
/// Raised when the sabotage tool is finished removing
/// </summary>
public sealed class SabotageToolRemoveEvent(EntityUid user, EntityUid target) : EntityEventArgs
{
    /// <summary>
    /// The entity ejecting the tool
    /// </summary>
    public readonly EntityUid User = user;

    /// <summary>
    /// The entity getting (un)sabotaged
    /// </summary>
    public readonly EntityUid Target = target;
}

/// <summary>
/// Raised by construction graphs to indicate the tool container should open
/// </summary>
public sealed class SabotagableMachineOpenedEvent : EntityEventArgs;

/// <summary>
/// Raised when the sabotage process is complete
/// </summary>
public sealed class SabotageCompleteEvent : EntityEventArgs;

/// <summary>
/// Used by <see cref=SabotagableMachineSystem/> for the insertion doafter
/// </summary>
/// <remarks>
/// DO NOT USE anywhere else
/// </remarks>
[Serializable, NetSerializable]
public sealed partial class SabotageToolInsertDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Used by <see cref=SabotagableMachineSystem/> for the removal doafter
/// </summary>
/// <remarks>
/// DO NOT USE anywhere else
/// </remarks>
[Serializable, NetSerializable]
public sealed partial class SabotageToolRemoveDoAfterEvent : SimpleDoAfterEvent;
