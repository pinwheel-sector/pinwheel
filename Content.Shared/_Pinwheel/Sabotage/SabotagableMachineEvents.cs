using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Pinwheel.Sabotage;

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

[Serializable, NetSerializable]
public sealed partial class SabotageToolInsertDoAfterEvent : SimpleDoAfterEvent
{}

[Serializable, NetSerializable]
public sealed partial class SabotageToolRemoveDoAfterEvent : SimpleDoAfterEvent
{}
