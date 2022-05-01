using Robust.Client.GameObjects;
using Content.Shared.Mail;

namespace Content.Client.Mail
{
    public sealed class MailSystem : VisualizerSystem<MailVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, MailVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (TryComp(uid, out SpriteComponent? sprite)
                && args.Component.TryGetData(MailVisuals.IsLocked, out bool isLocked))
            {
                sprite.LayerSetVisible(MailVisualLayers.IsLocked, isLocked);
            }
        }
    }
}

public enum MailVisualLayers : byte
{
    IsLocked
}
