using System.Globalization;
using System.Linq;
using System.Threading;
using Content.Server.Access.Systems;
using Content.Server.AlertLevel;
using Content.Server.Chat.Managers;
using Content.Server.Power.Components;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared.Communications;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Communications
{
    // TODO: ECS
    [RegisterComponent]
    public sealed class CommunicationsConsoleComponent : SharedCommunicationsConsoleComponent, IEntityEventSubscriber
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;

        private bool Powered => !_entities.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        private RoundEndSystem RoundEndSystem => EntitySystem.Get<RoundEndSystem>();
        private AlertLevelSystem AlertLevelSystem => EntitySystem.Get<AlertLevelSystem>();
        private StationSystem StationSystem => EntitySystem.Get<StationSystem>();

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(CommunicationsConsoleUiKey.Key);

        public TimeSpan LastAnnounceTime { get; private set; } = TimeSpan.Zero;
        public TimeSpan AnnounceCooldown { get; } = TimeSpan.FromSeconds(90);
        private CancellationTokenSource _announceCooldownEndedTokenSource = new();

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnOnReceiveMessage;
            }

            _entityManager.EventBus.SubscribeEvent<RoundEndSystemChangedEvent>(EventSource.Local, this, (s) => UpdateBoundInterface());
            _entityManager.EventBus.SubscribeEvent<AlertLevelChangedEvent>(EventSource.Local, this, _ => UpdateBoundInterface());
            _entityManager.EventBus.SubscribeEvent<AlertLevelDelayFinishedEvent>(EventSource.Local, this, _ => UpdateBoundInterface());
        }

        protected override void Startup()
        {
            base.Startup();

            UpdateBoundInterface();
        }

        private void UpdateBoundInterface()
        {
            if (!Deleted)
            {
                var system = RoundEndSystem;

                List<string>? levels = null;
                string currentLevel = default!;
                float currentDelay = 0;
                var stationUid = StationSystem.GetOwningStation(Owner);
                if (stationUid != null)
                {
                    if (_entityManager.TryGetComponent(stationUid.Value, out AlertLevelComponent? alerts)
                        && alerts.AlertLevels != null)
                    {
                        if (alerts.IsSelectable)
                        {
                            levels = new();
                            foreach (var (id, detail) in alerts.AlertLevels.Levels)
                            {
                                if (detail.Selectable)
                                {
                                    levels.Add(id);
                                }
                            }
                        }

                        currentLevel = alerts.CurrentLevel;
                        currentDelay = AlertLevelSystem.GetAlertLevelDelay(stationUid.Value, alerts);
                    }
                }

                UserInterface?.SetState(new CommunicationsConsoleInterfaceState(CanAnnounce(), system.CanCall(), levels, currentLevel, currentDelay, system.ExpectedCountdownEnd));
            }
        }

        public bool CanAnnounce()
        {
            if (LastAnnounceTime == TimeSpan.Zero)
            {
                return true;
            }
            return _gameTiming.CurTime >= LastAnnounceTime + AnnounceCooldown;
        }

        protected override void OnRemove()
        {
            _entityManager.EventBus.UnsubscribeEvent<RoundEndSystemChangedEvent>(EventSource.Local, this);
            base.OnRemove();
        }

        private void UserInterfaceOnOnReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case CommunicationsConsoleCallEmergencyShuttleMessage _:
                    RoundEndSystem.RequestRoundEnd(obj.Session.AttachedEntity);
                    break;

                case CommunicationsConsoleRecallEmergencyShuttleMessage _:
                    RoundEndSystem.CancelRoundEndCountdown(obj.Session.AttachedEntity);
                    break;
                case CommunicationsConsoleAnnounceMessage msg:
                    if (!CanAnnounce())
                    {
                        return;
                    }
                    _announceCooldownEndedTokenSource.Cancel();
                    _announceCooldownEndedTokenSource = new CancellationTokenSource();
                    LastAnnounceTime = _gameTiming.CurTime;
                    Timer.Spawn(AnnounceCooldown, UpdateBoundInterface, _announceCooldownEndedTokenSource.Token);
                    UpdateBoundInterface();

                    var message = msg.Message.Length <= 256 ? msg.Message.Trim() : $"{msg.Message.Trim().Substring(0, 256)}...";
                    var sys = _sysMan.GetEntitySystem<IdCardSystem>();

                    var author = "Unknown";
                    if (obj.Session.AttachedEntity is {Valid: true} mob && sys.TryFindIdCard(mob, out var id))
                    {
                        author = $"{id.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(id.JobTitle ?? string.Empty)})".Trim();
                    }

                    message += $"\nSent by {author}";
                    _chatManager.DispatchStationAnnouncement(message, "Communications Console", colorOverride: Color.Gold);
                    break;
                case CommunicationsConsoleSelectAlertLevelMessage alertMsg:
                    var stationUid = StationSystem.GetOwningStation(Owner);
                    if (stationUid != null)
                    {
                        AlertLevelSystem.SetLevel(stationUid.Value, alertMsg.Level, true, true);
                    }

                    break;
            }
        }
    }
}
