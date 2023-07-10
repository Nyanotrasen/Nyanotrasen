using Robust.Shared.Timing;
using Robust.Client.GameObjects;
using Content.Shared.Nyanotrasen.Clock;
using Content.Client.GameTicking.Managers;

namespace Content.Client.Nyanotrasen.Clock
{
    public sealed class ClockVisualsSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ClientGameTicker _ticker = default!;
        private TimeSpan _nextUpdate = TimeSpan.Zero;
        private TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_timing.CurTime < _nextUpdate)
                return;

            var stationTime = _timing.CurTime.Subtract(_ticker.RoundStartTimeSpan).Add(TimeSpan.FromHours(12));
            var minutes = stationTime.TotalMinutes % 60;
            var hours = stationTime.TotalHours % 12;

            Logger.Error("Minutes: " + minutes);
            Logger.Error("Hours: " + hours);

            var minuteAngle = Angle.FromDegrees((minutes / 60) * 360);
            var hourAngle = Angle.FromDegrees((hours / 12) * 360);

            foreach (var (_, sprite) in EntityQuery<ClockComponent, SpriteComponent>())
            {
                sprite.LayerSetRotation(ClockVisualLayers.MinuteHand, Angle.FromDegrees(90));
                sprite.LayerSetRotation(ClockVisualLayers.HourHand, hourAngle);
            }
            _nextUpdate = _timing.CurTime + UpdateInterval;
        }

        public enum ClockVisualLayers : byte
        {
            HourHand,
            MinuteHand
        }
    }
}
