using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Pinwheel.AlienRock.Equipment;

/// <summary>
/// Handheld tool displaying the nodes present on an artifact
/// </summary>
[RegisterComponent]
public sealed partial class AlienScannerComponent : Component
{
    /// <summary>
    /// Update rate of the UI controller
    /// </summary>
    [DataField]
    public TimeSpan UiUpdateRate = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long it takes to scan an artifact
    /// </summary>
    [DataField]
    public TimeSpan DoAfterLength = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Start to play when beginning the doafter
    /// </summary>
    [DataField]
    public SoundSpecifier SoundStart;

    /// <summary>
    /// Start to play when completing the doafter
    /// </summary>
    [DataField]
    public SoundSpecifier SoundEnd;
}

/// <summary>
/// Marker component holding data for an active <see cref="AlienScannerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class AlienScannerConnectedComponent : Component
{
    /// <summary>
    /// Rock the scanner is currently scanning
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Attached;

    /// <summary>
    /// Maximum range from the artifact, in tiles, before we disconnect
    /// </summary>
    [DataField]
    public int Range = 3;

    /// <summary>
    /// Update rate of checking range from attached artifact
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Next UI update time
    /// </summary>
    [DataField]
    public TimeSpan UpdateNext = TimeSpan.Zero;
}

/// <summary>
/// Marker component applied to scanned artifacts to clean up <see cref="AlienScannerConnectedComponent"/> on deletion
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class AlienRockScannedComponent : Component
{
    /// <summary>
    /// Scanner the rock is being scanned by
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Attached;
}

[Serializable, NetSerializable]
public enum AlienScannerUiKey : byte
{
    Key
}
