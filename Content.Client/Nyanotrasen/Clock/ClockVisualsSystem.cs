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
        private TimeSpan UpdateInterval = TimeSpan.FromSeconds(60);
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_timing.CurTime < _nextUpdate)
                return;

            var stationTime = _timing.CurTime.Subtract(_ticker.RoundStartTimeSpan).Add(TimeSpan.FromHours(12));
            var minutes = stationTime.TotalMinutes % 60;
            var hours = stationTime.TotalHours % 12;

            var minuteAngle = Angle.FromDegrees(360 - Math.Round(minutes / 60 * 360));
            var hourAngle = Angle.FromDegrees(360 - Math.Round(hours / 12 * 360));

            foreach (var (clock, sprite) in EntityQuery<AnalogueClockVisualsComponent, SpriteComponent>())
            {
                sprite.LayerSetRotation(ClockVisualLayers.MinuteHand, minuteAngle);

                var minuteX = clock.Origin.X * Math.Cos(minuteAngle) - clock.Origin.Y * Math.Sin(minuteAngle);
                var minuteY = clock.Origin.Y * Math.Cos(minuteAngle) + clock.Origin.X * Math.Sin(minuteAngle);

                var minuteVector = new Vector2((float) ((clock.Origin.X - minuteX) / 32), (float) ((clock.Origin.Y - minuteY) / 32));

                minuteVector.X = minuteAngle.Degrees switch
                {
                    <= 90 => minuteVector.X,
                    <= 180 => minuteVector.X + (float) ((minuteAngle.Degrees - 90) / 90) / 32,
                    <= 270 => minuteVector.X + 0.03125f,
                    _ => minuteVector.X + (float) (1 - (minuteAngle.Degrees - 270) / 90) / 32
                };

                minuteVector.Y = minuteAngle.Degrees switch
                {
                    <= 90 => minuteVector.Y - (float) (minuteAngle.Degrees / 90) / 32,
                    <= 180 => minuteVector.Y - 0.03125f,
                    <= 270 => minuteVector.Y - (float) ((1 - (minuteAngle.Degrees - 180) / 90) / 32),
                    _ => minuteVector.Y
                };

                sprite.LayerSetOffset(ClockVisualLayers.MinuteHand, minuteVector);

                sprite.LayerSetRotation(ClockVisualLayers.HourHand, hourAngle);
                var hourX = clock.Origin.X * Math.Cos(hourAngle) - clock.Origin.Y * Math.Sin(hourAngle);
                var hourY = clock.Origin.Y * Math.Cos(hourAngle) + clock.Origin.X * Math.Sin(hourAngle);
                var hourVector = new Vector2((float) ((clock.Origin.X - hourX) / 32), (float) ((clock.Origin.Y - hourY) / 32));

                hourVector.X = hourAngle.Degrees switch
                {
                    <= 90 => hourVector.X,
                    <= 180 => hourVector.X + (float) ((hourAngle.Degrees - 90) / 90) / 32,
                    <= 270 => hourVector.X + 0.03125f,
                    _ => hourVector.X + (float) (1 - (hourAngle.Degrees - 270) / 90) / 32
                };

                hourVector.Y = hourAngle.Degrees switch
                {
                    <= 90 => hourVector.Y - (float) (hourAngle.Degrees / 90) / 32,
                    <= 180 => hourVector.Y - 0.03125f,
                    <= 270 => hourVector.Y - (float) ((1 - (hourAngle.Degrees - 180) / 90) / 32),
                    _ => hourVector.Y
                };


                sprite.LayerSetOffset(ClockVisualLayers.HourHand, hourVector);
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
