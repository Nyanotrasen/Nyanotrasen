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
            // var minutes = stationTime.TotalMinutes % 60;
            var minutes = stationTime.TotalSeconds % 60;
            var hours = stationTime.TotalHours % 12;

            var minuteAngle = Angle.FromDegrees(Math.Round(minutes / 60 * 360));
            var hourAngle = Angle.FromDegrees(0 - (hours / 12 * 360));

            foreach (var (clock, sprite) in EntityQuery<AnalogueClockVisualsComponent, SpriteComponent>())
            {
                sprite.LayerSetRotation(ClockVisualLayers.MinuteHand, minuteAngle);

                var minuteX = clock.Origin.X * Math.Cos(minuteAngle) - clock.Origin.Y * Math.Sin(minuteAngle);
                var minuteY = clock.Origin.Y * Math.Cos(minuteAngle) + clock.Origin.X * Math.Sin(minuteAngle);

                Logger.Error("X and Y vectors: " + minuteX + " " + minuteY);

                var minuteVector = new Vector2((float) ((clock.Origin.X - minuteX) / 32), (float) ((clock.Origin.Y - minuteY) / 32));

                minuteVector.X = minuteAngle.Degrees switch
                {
                    <= 90 => minuteVector.X,
                    <= 180 => minuteVector.X + (float) ((minuteAngle.Degrees - 90) / 90) / 32,
                    <= 270 => minuteVector.X, // There is a relationship here left to figure out
                    _ => minuteVector.X + (float) (1 - (minuteAngle.Degrees - 270) / 90) / 32
                };

                Logger.Error("degrees: " + minuteAngle.Degrees);
                minuteVector.Y = minuteAngle.Degrees switch
                {
                    <= 90 => minuteVector.Y - (float) (minuteAngle.Degrees / 90) / 32,
                    <= 180 => minuteVector.Y, // And there is a relationship here left to figure out
                    <= 270 => minuteVector.Y - (float) ((1 - (minuteAngle.Degrees - 270) / 90) / 32),
                    _ => minuteVector.Y
                };

                sprite.LayerSetOffset(ClockVisualLayers.MinuteHand, minuteVector);
                Logger.Error("Minute offset: " + minuteVector);

                sprite.LayerSetRotation(ClockVisualLayers.HourHand, hourAngle);
                var hourX = clock.Origin.X * Math.Cos(hourAngle) - clock.Origin.Y * Math.Sin(hourAngle);
                var hourY = clock.Origin.Y * Math.Cos(hourAngle) + clock.Origin.X * Math.Sin(hourAngle);
                var hourVector = new Vector2((float) ((clock.Origin.X - hourX) / 32), (float) ((clock.Origin.Y - hourY) / 32));
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
