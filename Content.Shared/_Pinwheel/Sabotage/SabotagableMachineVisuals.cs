using Robust.Shared.Serialization;

/// <summary>
/// Enums used for sabotagable machine visuals
/// </summary>
namespace Content.Shared._Pinwheel.Sabotage
{
    [Serializable, NetSerializable]
    public enum SabotagableMachineVisuals : byte
    {
        ShellState, // the cover for the cointainer
        ToolState, // the tool
        LightState // the indicators
    }

    [Serializable, NetSerializable]
    public enum SabotagableMachineVisualLayers : int
    {
        Label, // markings on the shell
        Cover, // the shell itself
        Tool, // the star of the show
        Lights // "something is wrong" indicators
    }
}
