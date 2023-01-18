using Content.Client.HealthOverlay;
using Content.Shared.SimpleStation14.Clothing;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Client.GameObjects;

namespace Content.Client.SimpleStation14.Overlays
{
    public sealed class HealthGlassesSystem : EntitySystem
    {
        [Dependency] private readonly HealthOverlaySystem _healthOverlay = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HealthGlassesComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<HealthGlassesComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<HealthGlassesComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<HealthGlassesComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnInit(EntityUid uid, HealthGlassesComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _healthOverlay.OrganicsOnly = true;
                _healthOverlay.CheckLOS = true;
                _healthOverlay.Enabled = true;
            }


        }
        private void OnRemove(EntityUid uid, HealthGlassesComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _healthOverlay.OrganicsOnly = false;
                _healthOverlay.CheckLOS = false;
                _healthOverlay.Enabled = false;
            }
        }

        private void OnPlayerAttached(EntityUid uid, HealthGlassesComponent component, PlayerAttachedEvent args)
        {
            _healthOverlay.OrganicsOnly = true;
            _healthOverlay.CheckLOS = true;
            _healthOverlay.Enabled = true;
        }

        private void OnPlayerDetached(EntityUid uid, HealthGlassesComponent component, PlayerDetachedEvent args)
        {
            _healthOverlay.OrganicsOnly = false;
            _healthOverlay.CheckLOS = false;
            _healthOverlay.Enabled = false;
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            _healthOverlay.OrganicsOnly = false;
            _healthOverlay.CheckLOS = false;
            _healthOverlay.Enabled = false;
        }
    }
}
