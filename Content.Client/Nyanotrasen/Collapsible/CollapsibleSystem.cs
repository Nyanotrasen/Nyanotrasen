using Robust.Client.GameObjects;
using Content.Shared.Collapsible;

namespace Content.Client.Collapsible
{
    public sealed class CollapsibleSystem : VisualizerSystem<CollapsibleVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, CollapsibleVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(CollapsibleVisuals.IsCollapsed, out bool isCollapsed))
            {
                var state = isCollapsed ? component.CollapsedState : component.ExtendedState;
                sprite.LayerSetState(CollapsibleVisualLayers.IsCollapsed, state);
            }
        }
    }
}

public enum CollapsibleVisualLayers : byte
{
    IsCollapsed
}
