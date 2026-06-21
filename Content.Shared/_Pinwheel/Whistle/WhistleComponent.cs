using Robust.Shared.GameStates;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared._Pinwheel.Whistle;

/// <summary>
/// On action or use, plays a sound and spawns an entity attached to all entities with <see cref="HumanoidAppearanceComponent"/> in range.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WhistleComponent : Component
{
    /// <summary>
    /// Entity prototype to spawn
    /// </summary>
    [DataField]
    public EntProtoId Effect = "WhistleExclamation";

    /// <summary>
    /// Range value.
    /// </summary>
    [DataField]
    public float Distance = 0;

    /// <summary>
    /// Entity prototype for the whistling action
    /// </summary>
    [DataField]
    public EntProtoId ActionId = "ActionWhistle";

    [DataField]
    public EntityUid? Action;

    [DataField]
    public SoundSpecifier WhistleSound = new SoundCollectionSpecifier("PlasticWhistle");
}
