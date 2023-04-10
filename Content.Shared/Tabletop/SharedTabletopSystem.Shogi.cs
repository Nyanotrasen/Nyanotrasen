using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;

namespace Content.Shared.Tabletop
{
    public abstract partial class SharedTabletopSystem : EntitySystem
    {
        private void InitializeShogi()
        {
            SubscribeAllEvent<TabletopUseSecondaryEvent>(OnShogiUseSecondary);
            SubscribeAllEvent<TabletopActivateInWorldEvent>(OnShogiActivateInWorld);
            SubscribeAllEvent<TabletopDraggingPlayerChangedEvent>(OnShogiDraggingPlayerChanged);
        }

        private void OnShogiUseSecondary(TabletopUseSecondaryEvent msg, EntitySessionEventArgs args)
        {
            if (!_gameTiming.IsFirstTimePredicted)
                return;

            if (!TryComp<TabletopShogiPieceComponent>(msg.UsedEntityUid, out var component))
                return;

            if (!component.CanPromote)
                return;

            _appearance.TryGetData<bool>(msg.UsedEntityUid, ShogiPieceVisuals.IsPromoted, out var isPromoted);
            _appearance.SetData(msg.UsedEntityUid, ShogiPieceVisuals.IsPromoted, !isPromoted);
        }

        private void OnShogiActivateInWorld(TabletopActivateInWorldEvent msg, EntitySessionEventArgs args)
        {
            if (!_gameTiming.IsFirstTimePredicted)
                return;

            if (!HasComp<TabletopShogiPieceComponent>(msg.ActivatedEntityUid))
                return;

            var angle = _transforms.GetWorldRotation(msg.ActivatedEntityUid) - Angle.FromDegrees(180);
            _transforms.SetWorldRotation(msg.ActivatedEntityUid, angle);
        }

        private void OnShogiDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg, EntitySessionEventArgs args)
        {
            if (!_gameTiming.IsFirstTimePredicted)
                return;

            if (msg.IsDragging == true)
                return;

            if (args.SenderSession.AttachedEntity is not { Valid: true } playerEntity)
                return;

            if (!TryComp<TabletopShogiPieceComponent>(msg.DraggedEntityUid, out var component))
                return;

            // Play the signature sound.
            var clack = new SoundPathSpecifier("/Audio/Nyanotrasen/shogi_piece_clack.ogg");
            _audio.PlayPredicted(clack, playerEntity, playerEntity, AudioParams.Default.WithVariation(0.06f));
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
