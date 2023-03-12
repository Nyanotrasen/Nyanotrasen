using Robust.Shared.Serialization;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;

namespace Content.Shared.Tabletop
{
    public abstract partial class SharedTabletopSystem
    {
        private void InitializeShogi()
        {
            SubscribeAllEvent<TabletopUseSecondaryEvent>(OnShogiUseSecondary);
            SubscribeAllEvent<TabletopActivateInWorldEvent>(OnShogiActivateInWorld);
        }

        private void OnShogiUseSecondary(TabletopUseSecondaryEvent msg, EntitySessionEventArgs args)
        {
            if (!HasComp<TabletopShogiPieceComponent>(msg.UsedEntityUid))
                return;

            var angle = _transforms.GetWorldRotation(msg.UsedEntityUid) - Angle.FromDegrees(180);
            _transforms.SetWorldRotation(msg.UsedEntityUid, angle);
        }

        private void OnShogiActivateInWorld(TabletopActivateInWorldEvent msg, EntitySessionEventArgs args)
        {
            if (!TryComp<TabletopShogiPieceComponent>(msg.ActivatedEntityUid, out var component))
                return;

            if (!component.CanPromote)
                return;

            _appearance.TryGetData<bool>(msg.ActivatedEntityUid, ShogiPieceVisuals.IsPromoted, out var isPromoted);
            _appearance.SetData(msg.ActivatedEntityUid, ShogiPieceVisuals.IsPromoted, !isPromoted);
        }
    }

    [Serializable, NetSerializable]
    public enum ShogiPieceVisuals : byte
    {
        IsPromoted,
    }

    public enum ShogiPieceVisualLayers : byte
    {
        Unpromoted,
        Promoted,
    }
}
