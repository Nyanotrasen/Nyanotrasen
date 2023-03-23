using Robust.Client.GameObjects;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;

namespace Content.Client.Tabletop
{
    public sealed class TabletopShogiPieceSystem : VisualizerSystem<TabletopShogiPieceComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, TabletopShogiPieceComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null ||
                !AppearanceSystem.TryGetData<bool>(uid, ShogiPieceVisuals.IsPromoted, out var isPromoted, args.Component))
            {
                return;
            }

            args.Sprite.LayerSetVisible(ShogiPieceVisualLayers.Promoted, isPromoted);
        }
    }
}
