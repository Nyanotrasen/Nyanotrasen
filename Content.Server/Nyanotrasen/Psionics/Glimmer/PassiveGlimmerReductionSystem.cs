using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.GameTicking;
using Content.Server.CartridgeLoader.Cartridges;

namespace Content.Server.Psionics.Glimmer
{
    /// <summary>
    /// Handles the passive reduction of glimmer.
    /// </summary>
    public sealed class PassiveGlimmerReductionSystem : EntitySystem
    {
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly GlimmerMonitorCartridgeSystem _cartridgeSys = default!;

        /// List of glimmer values spaced by minute.
        public List<int> GlimmerValues = new();

        public TimeSpan TargetUpdatePeriod = TimeSpan.FromSeconds(6);

        /// Every 10 updates a minute will pass and we'll log the value at the time.
        private int _i;
        public TimeSpan NextUpdateTime = default!;
        public TimeSpan LastUpdateTime = default!;

        private float _glimmerLostPerSecond;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            _cfg.OnValueChanged(CCVars.GlimmerLostPerSecond, UpdatePassiveGlimmer, true);
        }

        private void Reset(RoundRestartCleanupEvent args)
        {
            GlimmerValues.Clear();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _timing.CurTime;
            if (NextUpdateTime > curTime)
                return;

            _i++;

            var delta = curTime - LastUpdateTime;
            var maxGlimmerLost = (int) Math.Round(delta.TotalSeconds * _glimmerLostPerSecond);

            // It used to be 75% to lose one glimmer per ten seconds, but now it's 50% per six seconds.
            // The probability is exactly the same over the same span of time. (0.25 ^ 3 == 0.5 ^ 6)
            // This math is just easier to do for pausing's sake.
            var actualGlimmerLost = _random.Next(0, 1 + maxGlimmerLost);

            _glimmerSystem.Glimmer -= actualGlimmerLost;


            if (_i == 1)
            {
                GlimmerValues.Add(_glimmerSystem.Glimmer);

                _i = 0;
            }

            NextUpdateTime = curTime + TargetUpdatePeriod;
            LastUpdateTime = curTime;
        }

        private void UpdatePassiveGlimmer(float value) => _glimmerLostPerSecond = value;
    }
}
