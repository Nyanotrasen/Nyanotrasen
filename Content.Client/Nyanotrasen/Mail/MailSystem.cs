using Robust.Client.GameObjects;
using Content.Shared.Mail;

namespace Content.Client.Mail
{
    public sealed class MailSystem : VisualizerSystem<MailVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, MailVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(MailVisuals.IsLocked, out bool isLocked)
                && args.Component.TryGetData(MailVisuals.IsTrash, out bool isTrash))
            {
                var state = isTrash ? component.TrashState : component.NormalState;
                sprite.LayerSetVisible(MailVisualLayers.IsLocked, isLocked);
                sprite.LayerSetState(MailVisualLayers.IsTrash, state);
            }
        }
    }
}

public enum MailVisualLayers : byte
{
    IsLocked,
    IsTrash
}
