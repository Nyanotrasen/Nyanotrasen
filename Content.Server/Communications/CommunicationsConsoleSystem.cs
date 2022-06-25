using System.Globalization;
using Content.Server.Access.Systems;
using Content.Server.AlertLevel;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Communications;
using Robust.Shared.Player;

namespace Content.Server.Communications
{
    public sealed class CommunicationsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        private const int MaxMessageLength = 256;

        public override void Initialize()
        {
            // All events that refresh the BUI
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            SubscribeLocalEvent<CommunicationsConsoleComponent, ComponentInit>((_, comp, _) => UpdateBoundUserInterface(comp));
            SubscribeLocalEvent<RoundEndSystemChangedEvent>(_ => OnGenericBroadcastEvent());
            SubscribeLocalEvent<AlertLevelDelayFinishedEvent>(_ => OnGenericBroadcastEvent());

            // Messages from the BUI
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertLevelMessage>(OnSelectAlertLevelMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleAnnounceMessage>(OnAnnounceMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallEmergencyShuttleMessage>(OnCallShuttleMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallEmergencyShuttleMessage>(OnRecallShuttleMessage);
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityQuery<CommunicationsConsoleComponent>())
            {
                // TODO: Find a less ass way of refreshing the UI
                if (comp.AlreadyRefreshed) continue;
                if (comp.AnnouncementCooldownRemaining <= 0f)
                {
                    UpdateBoundUserInterface(comp);
                    comp.AlreadyRefreshed = true;
                    continue;
                }
                comp.AnnouncementCooldownRemaining -= frameTime;
            }

            base.Update(frameTime);
        }

        /// <summary>
        /// Update the UI of every comms console.
        /// </summary>
        private void OnGenericBroadcastEvent()
        {
            foreach (var comp in EntityQuery<CommunicationsConsoleComponent>())
            {
                UpdateBoundUserInterface(comp);
            }
        }

        /// <summary>
        /// Updates all comms consoles belonging to the station that the alert level was set on
        /// </summary>
        /// <param name="args">Alert level changed event arguments</param>
        private void OnAlertLevelChanged(AlertLevelChangedEvent args)
        {
            foreach (var comp in EntityQuery<CommunicationsConsoleComponent>())
            {
                var entStation = _stationSystem.GetOwningStation(comp.Owner);
                if (args.Station == entStation)
                {
                    UpdateBoundUserInterface(comp);
                }
            }
        }

        private void UpdateBoundUserInterface(CommunicationsConsoleComponent comp)
        {
            var uid = comp.Owner;

            var stationUid = _stationSystem.GetOwningStation(uid);
            List<string>? levels = null;
            string currentLevel = default!;
            float currentDelay = 0;

            if (stationUid != null)
            {
                if (TryComp(stationUid.Value, out AlertLevelComponent? alertComp) &&
                    alertComp.AlertLevels != null)
                {
                    if (alertComp.IsSelectable)
                    {
                        levels = new();
                        foreach (var (id, detail) in alertComp.AlertLevels.Levels)
                        {
                            if (detail.Selectable)
                            {
                                levels.Add(id);
                            }
                        }
                    }

                    currentLevel = alertComp.CurrentLevel;
                    currentDelay = _alertLevelSystem.GetAlertLevelDelay(stationUid.Value, alertComp);
                }
            }

            comp.UserInterface?.SetState(
                new CommunicationsConsoleInterfaceState(
                    CanAnnounce(comp),
                    CanCall(comp),
                    levels,
                    currentLevel,
                    currentDelay,
                    _roundEndSystem.ExpectedCountdownEnd
                    )
                );
        }

        private bool CanAnnounce(CommunicationsConsoleComponent comp)
        {
            return comp.AnnouncementCooldownRemaining <= 0f;
        }

        private bool CanUse(EntityUid user, EntityUid console)
        {
            if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent) && accessReaderComponent.Enabled)
            {
                return _accessReaderSystem.IsAllowed(user, accessReaderComponent);
            }
            return true;
        }

        private bool CanCall(CommunicationsConsoleComponent comp)
        {
            return comp.CanCallShuttle && _roundEndSystem.CanCall();
        }

        private void OnSelectAlertLevelMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleSelectAlertLevelMessage message)
        {
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupCursor(Loc.GetString("comms-console-permission-denied"), Filter.Entities(mob));
                return;
            }

            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
            {
                _alertLevelSystem.SetLevel(stationUid.Value, message.Level, true, true);
            }
        }

        private void OnAnnounceMessage(EntityUid uid, CommunicationsConsoleComponent comp,
            CommunicationsConsoleAnnounceMessage message)
        {
            var msg = message.Message.Length <= MaxMessageLength ? message.Message.Trim() : $"{message.Message.Trim().Substring(0, MaxMessageLength)}...";
            var author = Loc.GetString("comms-console-announcement-unknown-sender");
            if (message.Session.AttachedEntity is {Valid: true} mob)
            {
                if (!CanAnnounce(comp))
                {
                    return;
                }

                if (!CanUse(mob, uid))
                {
                    _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, Filter.Entities(mob));
                    return;
                }

                if (_idCardSystem.TryFindIdCard(mob, out var id))
                {
                    author = $"{id.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.JobTitle ?? string.Empty)})".Trim();
                }
            }

            comp.AnnouncementCooldownRemaining = comp.DelayBetweenAnnouncements;
            comp.AlreadyRefreshed = false;
            UpdateBoundUserInterface(comp);

            // allow admemes with vv
            Loc.TryGetString(comp.AnnouncementDisplayName, out var title);
            title ??= comp.AnnouncementDisplayName;

            msg += "\n" + Loc.GetString("comms-console-announcement-sent-by") + " " + author;
            if (comp.AnnounceGlobal)
            {
                _chatSystem.DispatchGlobalStationAnnouncement(msg, title, colorOverride: comp.AnnouncementColor);
                return;
            }
            _chatSystem.DispatchStationAnnouncement(uid, msg, title, colorOverride: comp.AnnouncementColor);
        }

        private void OnCallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleCallEmergencyShuttleMessage message)
        {
            if (!comp.CanCallShuttle) return;
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, Filter.Entities(mob));
                return;
            }
            _roundEndSystem.RequestRoundEnd(uid);
        }

        private void OnRecallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleRecallEmergencyShuttleMessage message)
        {
            if (!comp.CanCallShuttle) return;
            if (message.Session.AttachedEntity is not {Valid: true} mob) return;
            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, Filter.Entities(mob));
                return;
            }
            _roundEndSystem.CancelRoundEndCountdown(uid);
        }
    }
}
