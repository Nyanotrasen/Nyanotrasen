using Robust.Client.GameObjects;
using Content.Shared.Psionics.Glimmer;

namespace Content.Client.Psionics.Glimmer
{
    /// <summary>
    /// This system uses <see cref="VisualizerSystem"/> to update the
    /// GlimmerTier layer of any Entity that has the GlimmerReactive Component.
    ///
    /// By virtue of using VisualizerSystem, those entities need to have the
    /// Appearance Component as well.
    ///
    /// <see cref="GlimmerTier"/> contains the number of states needed for each sprite,
    /// excluding Minimal (0), which is not displayed. Visible states start at 1,
    /// such as <c>glimmer_1</c> to <c>glimmer_2</c> and so on.
    /// </summary>
    public sealed class GlimmerReactiveVisualizerSystem : VisualizerSystem<SharedGlimmerReactiveComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, SharedGlimmerReactiveComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (args.Component.TryGetData(GlimmerReactiveVisuals.GlimmerTier, out int tier))
            {
                if (tier != 0)
                {
                    args.Sprite.LayerSetState(GlimmerReactiveVisualLayers.GlimmerTier, $"glimmer_{tier}");
                    args.Sprite.LayerSetVisible(GlimmerReactiveVisualLayers.GlimmerTier, true);
                } else {
                    args.Sprite.LayerSetVisible(GlimmerReactiveVisualLayers.GlimmerTier, false);
                }
            }
        }
    }

    public enum GlimmerReactiveVisualLayers : byte
    {
        GlimmerTier,
    }
}
