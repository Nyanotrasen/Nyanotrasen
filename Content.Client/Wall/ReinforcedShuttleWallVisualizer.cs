using Content.Shared.Wall;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Wall
{
    [UsedImplicitly]
    public sealed class ReinforcedShuttleWallVisualizer : AppearanceVisualizer
    {
        [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (component.TryGetData(ReinforcedShuttleWallVisuals.DeconstructionStage, out int stage))
            {
                SetDeconstructionStage(component, stage);
            }
        }

        public void SetDeconstructionStage(AppearanceComponent component, int stage)
        {
            var entity = component.Owner;

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(entity, out SpriteComponent? sprite)) return;

            var index = sprite.LayerMapReserveBlank(ReinforcedShuttleWallVisualLayers.Deconstruction);

            if (stage < 0)
            {
                sprite.LayerSetVisible(index, false);
                return;
            }

            sprite.LayerSetVisible(index, true);
            sprite.LayerSetState(index, $"shuttle_construct-{stage}");
        }
    }

    public enum ReinforcedShuttleWallVisualLayers : byte
    {
        Deconstruction,
    }
}
