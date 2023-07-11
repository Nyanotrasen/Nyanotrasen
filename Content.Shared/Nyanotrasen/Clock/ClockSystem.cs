using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Shared.Nyanotrasen.Clock
{
    public sealed class ClockSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        private TimeSpan _roundStart;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ClockComponent, ExaminedEvent>(OnExamined);
            SubscribeNetworkEvent<TickerLobbyStatusEvent>(LobbyStatus);
        }

        private void OnExamined(EntityUid uid, ClockComponent component, ExaminedEvent args)
        {
            if (component.ShowSeconds)
                args.PushMarkup(Loc.GetString("clock-examined", ("clock", uid), ("time", GetStationTime().Time.ToString("hh\\:mm\\:ss"))));
            else
                args.PushMarkup(Loc.GetString("clock-examined", ("clock", uid), ("time", GetStationTime().Time.ToString("hh\\:mm"))));
        }

        private void LobbyStatus(TickerLobbyStatusEvent ev)
        {
            _roundStart = ev.RoundStartTimeSpan;
        }

        public (TimeSpan Time, int Date) GetStationTime()
        {
            var stationTime = _timing.CurTime.Subtract(_roundStart).Add(TimeSpan.FromHours(12));

            var date = 13;
            while (stationTime.TotalHours >= 24)
            {
                stationTime.Subtract(TimeSpan.FromHours(24));
                date = date + 1;
            }

            return (stationTime, date);
        }

        public string GetDate()
        {
            // please tell me you guys aren't gonna have a 4 week round yet...
            return Loc.GetString("standard-date", ("date", GetStationTime().Date));
        }
    }
}
