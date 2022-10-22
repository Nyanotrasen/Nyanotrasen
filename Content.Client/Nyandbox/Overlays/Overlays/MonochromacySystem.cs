using Content.Shared.Abilities;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Nyandbox.Overlays;
public sealed class MonochromacySystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private MonochromacyOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MonochromacyComponent, ComponentInit>(OnMonochromacyInit);
        SubscribeLocalEvent<MonochromacyComponent, ComponentShutdown>(OnMonochromacyShutdown);

        SubscribeLocalEvent<MonochromacyComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MonochromacyComponent, PlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnPlayerAttached(EntityUid uid, MonochromacyComponent component, PlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, MonochromacyComponent component, PlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnMonochromacyInit(EntityUid uid, MonochromacyComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnMonochromacyShutdown(EntityUid uid, MonochromacyComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
