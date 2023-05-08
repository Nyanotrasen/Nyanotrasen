using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.Psionics.Glimmer
{
    public sealed class GlimmerSystem : EntitySystem
    {
        [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public const float GlimmerLostPerSecond = 0.1f;

        public TimeSpan TargetUpdatePeriod = TimeSpan.FromSeconds(6);
        public TimeSpan NextUpdateTime = default!;
        public TimeSpan LastUpdateTime = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _timing.CurTime;
            if (NextUpdateTime > curTime)
                return;

            var delta = curTime - LastUpdateTime;
            var maxGlimmerLost = (int) Math.Round(delta.TotalSeconds * GlimmerLostPerSecond);

            // It used to be 75% to lose one glimmer per ten seconds, but now it's 50% per six seconds.
            // The probability is exactly the same over the same span of time. (0.25 ^ 3 == 0.5 ^ 6)
            // This math is just easier to do for pausing's sake.
            var actualGlimmerLost = _random.Next(0, 1 + maxGlimmerLost);

            _sharedGlimmerSystem.Glimmer -= actualGlimmerLost;

            NextUpdateTime = curTime + TargetUpdatePeriod;
            LastUpdateTime = curTime;
        }
    }
}
