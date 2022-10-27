using Content.Server.Popups;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Speech;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.Player;

namespace Content.Server.Nyandbox.Traits.Mute
{
    public sealed class MuteTraitSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MuteTraitComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<MuteTraitComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        }

        private void OnComponentInit(EntityUid uid, MuteTraitComponent component, ComponentInit args)
        {
            _alertsSystem.ShowAlert(uid, AlertType.Muted);
        }
        private void OnSpeakAttempt(EntityUid uid, MuteTraitComponent component, SpeakAttemptEvent args)
        {
            if (!component.Enabled)
                return;

            _popupSystem.PopupEntity(Loc.GetString("mute-cant-speak"), uid, Filter.Entities(uid));
            args.Cancel();
        }
    }
}
