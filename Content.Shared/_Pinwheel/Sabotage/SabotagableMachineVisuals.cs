using Robust.Shared.Serialization;

/// <summary>
/// Enums used for sabotagable machine lid and fill visuals
/// </summary>
namespace Content.Shared._Pinwheel.Sabotage
{
    [Serializable, NetSerializable]
    public enum SabotagableMachineVisuals : byte
    {
        ShellState,
        ToolState
    }

    [Serializable, NetSerializable]
    public enum SabotagableMachineVisualLayers : int
    {
        Label,
        Cover,
        Tool
    }
}
