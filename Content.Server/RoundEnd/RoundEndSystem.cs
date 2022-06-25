using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.RoundEnd
{
    public sealed class RoundEndSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        [Dependency] private readonly IAdminLogManager _adminLogger = default!;


        public TimeSpan DefaultCooldownDuration { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan DefaultCountdownDuration { get; set; } = TimeSpan.FromMinutes(4);
        public TimeSpan DefaultRestartRoundDuration { get; set; } = TimeSpan.FromMinutes(1);

        private CancellationTokenSource? _countdownTokenSource = null;
        private CancellationTokenSource? _cooldownTokenSource = null;
        public TimeSpan? ExpectedCountdownEnd { get; set; } = null;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Reset());
        }

        private void Reset()
        {
            if (_countdownTokenSource != null)
            {
                _countdownTokenSource.Cancel();
                _countdownTokenSource = null;
            }

            if (_cooldownTokenSource != null)
            {
                _cooldownTokenSource.Cancel();
                _cooldownTokenSource = null;
            }

            ExpectedCountdownEnd = null;
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
        }

        public bool CanCall()
        {
            return _cooldownTokenSource == null;
        }

        public void RequestRoundEnd(EntityUid? requester = null, bool checkCooldown = true)
        {
            RequestRoundEnd(DefaultCountdownDuration, requester, checkCooldown);
        }

        public void RequestRoundEnd(TimeSpan countdownTime, EntityUid? requester = null, bool checkCooldown = true)
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound) return;

            if (checkCooldown && _cooldownTokenSource != null) return;

            if (_countdownTokenSource != null) return;
            _countdownTokenSource = new();

            if (requester != null)
            {
                _adminLogger.Add(LogType.ShuttleCalled, LogImpact.High, $"Shuttle called by {ToPrettyString(requester.Value):user}");
            }
            else
            {
                _adminLogger.Add(LogType.ShuttleCalled, LogImpact.High, $"Shuttle called");
            }

            _chatSystem.DispatchGlobalStationAnnouncement(Loc.GetString("round-end-system-shuttle-called-announcement",("minutes", countdownTime.Minutes)), Loc.GetString("Station"), false, Color.Gold);

            SoundSystem.Play("/Audio/Announcements/shuttlecalled.ogg", Filter.Broadcast());

            ExpectedCountdownEnd = _gameTiming.CurTime + countdownTime;
            Timer.Spawn(countdownTime, EndRound, _countdownTokenSource.Token);

            ActivateCooldown();
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
        }

        public void CancelRoundEndCountdown(EntityUid? requester = null, bool checkCooldown = true)
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound) return;
            if (checkCooldown && _cooldownTokenSource != null) return;

            if (_countdownTokenSource == null) return;
            _countdownTokenSource.Cancel();
            _countdownTokenSource = null;

            if (requester != null)
            {
                _adminLogger.Add(LogType.ShuttleRecalled, LogImpact.High, $"Shuttle recalled by {ToPrettyString(requester.Value):user}");
            }
            else
            {
                _adminLogger.Add(LogType.ShuttleRecalled, LogImpact.High, $"Shuttle recalled");
            }

            _chatSystem.DispatchGlobalStationAnnouncement(Loc.GetString("round-end-system-shuttle-recalled-announcement"),
                Loc.GetString("Station"), false, colorOverride: Color.Gold);

            SoundSystem.Play("/Audio/Announcements/shuttlerecalled.ogg", Filter.Broadcast());

            ExpectedCountdownEnd = null;
            ActivateCooldown();
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
        }

        public void EndRound()
        {
            if (_gameTicker.RunLevel != GameRunLevel.InRound) return;
            ExpectedCountdownEnd = null;
            RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
            _gameTicker.EndRound();
            _countdownTokenSource?.Cancel();
            _countdownTokenSource = new();
            _chatManager.DispatchServerAnnouncement(Loc.GetString("round-end-system-round-restart-eta-announcement", ("minutes", DefaultRestartRoundDuration.Minutes)));
            Timer.Spawn(DefaultRestartRoundDuration, AfterEndRoundRestart, _countdownTokenSource.Token);
        }

        private void AfterEndRoundRestart()
        {
            if (_gameTicker.RunLevel != GameRunLevel.PostRound) return;
            Reset();
            _gameTicker.RestartRound();
        }

        private void ActivateCooldown()
        {
            _cooldownTokenSource?.Cancel();
            _cooldownTokenSource = new();
            Timer.Spawn(DefaultCooldownDuration, () =>
            {
                _cooldownTokenSource.Cancel();
                _cooldownTokenSource = null;
                RaiseLocalEvent(RoundEndSystemChangedEvent.Default);
            }, _cooldownTokenSource.Token);
        }
    }

    public sealed class RoundEndSystemChangedEvent : EntityEventArgs
    {
        public static RoundEndSystemChangedEvent Default { get; } = new();
    }
}
