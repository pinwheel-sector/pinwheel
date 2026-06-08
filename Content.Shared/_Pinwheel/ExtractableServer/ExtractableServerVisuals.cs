using Robust.Shared.Serialization;

namespace Content.Shared._Pinwheel.ExtractableServer
{
    [Serializable, NetSerializable]
    public enum ExtractableServerVisuals : byte
    {
        LidConstruction,
        ShellConstruction
    }

    [Serializable, NetSerializable]
    public enum ExtractableServerVisualLayers : byte
    {
        Label, // i hate that you can't use 1 enum for 2 layers
        Lid,
        Shell
    }
}
