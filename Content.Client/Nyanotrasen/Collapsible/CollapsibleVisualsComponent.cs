using Content.Shared.Hands.Components;
using static Robust.Shared.GameObjects.SharedSpriteComponent;

namespace Content.Client.Collapsible;

/// <summary>
/// Holds the visuals for collapsible items;
/// </summary>
[RegisterComponent]
public sealed class CollapsibleVisualsComponent : Component
{
    [DataField("collapsedState", required: true)]
    public string CollapsedState = default!;

    [DataField("extendedState", required: true)]
    public string ExtendedState = default!;

    /// <summary>
    ///     Layers to add to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    [DataField("inhandVisuals")]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();
}
