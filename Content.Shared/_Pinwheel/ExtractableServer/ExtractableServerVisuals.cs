using Robust.Shared.Serialization;

/// <summary>
/// Enums used purely in YAML for extractable servers
/// </summary>
namespace Content.Shared._Pinwheel.ExtractableServer
{
    [Serializable, NetSerializable]
    public enum ExtractableServerVisuals : byte
    {
        LidConstruction,
        ShellConstruction,
        Disk
    }

    [Serializable, NetSerializable]
    public enum ExtractableServerVisualLayers : byte
    {
        Label, // i hate that you can't use 1 enum for 2 layers
        Lid,
        Shell,
        Disk
    }
}
