using Content.Shared.EntityHealthBar;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Client.GameObjects;

namespace Content.Client.EntityHealthBar
{
    public sealed class ShowHealthBarsSystem : EntitySystem
    {
        [Dependency] private readonly EntityHealthBarSystem _healthOverlay = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowHealthBarsComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShowHealthBarsComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<ShowHealthBarsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ShowHealthBarsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnInit(EntityUid uid, ShowHealthBarsComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _healthOverlay.DamageContainer = component.DamageContainer;
                _healthOverlay.CheckLOS = component.CheckLOS;
                _healthOverlay.Enabled = true;
            }


        }
        private void OnRemove(EntityUid uid, ShowHealthBarsComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _healthOverlay.DamageContainer = null;
                _healthOverlay.CheckLOS = false;
                _healthOverlay.Enabled = false;
            }
        }

        private void OnPlayerAttached(EntityUid uid, ShowHealthBarsComponent component, PlayerAttachedEvent args)
        {
            _healthOverlay.DamageContainer = null;
            _healthOverlay.CheckLOS = component.CheckLOS;
            _healthOverlay.Enabled = true;
        }

        private void OnPlayerDetached(EntityUid uid, ShowHealthBarsComponent component, PlayerDetachedEvent args)
        {
            _healthOverlay.DamageContainer = null;
            _healthOverlay.CheckLOS = false;
            _healthOverlay.Enabled = false;
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            _healthOverlay.DamageContainer = null;
            _healthOverlay.CheckLOS = false;
            _healthOverlay.Enabled = false;
        }
    }
}
