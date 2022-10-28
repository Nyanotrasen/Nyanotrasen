using Robust.Shared.Serialization;
using Content.Shared.GameTicking;

namespace Content.Shared.Psionics.Glimmer
{
    /// <summary>
    /// How much psionic power is flowing around.
    /// 1 average power use = 10 glimmer.
    /// Passively lose 1 glimmer every 10 seconds.
    /// </summary>
    public sealed class SharedGlimmerSystem : EntitySystem
    {
        private int _glimmer = 0;
        public int Glimmer
        {
            get { return _glimmer; }
            set { _glimmer = Math.Clamp(value, 0, 1000); }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

        private void Reset(RoundRestartCleanupEvent args)
        {
            Glimmer = 0;
        }

        /// <summary>
        /// Return an abstracted range of a glimmer count.
        /// </summary>
        /// <param name="glimmer">What glimmer count to check. Uses the current glimmer by default.</param>
        public GlimmerTier GetGlimmerTier(int? glimmer = null)
        {
            if (glimmer == null)
                glimmer = Glimmer;

            return (glimmer) switch
            {
                <= 49 => GlimmerTier.Minimal,
                >= 50 and <= 99 => GlimmerTier.Low,
                >= 100 and <= 299 => GlimmerTier.Moderate,
                >= 300 and <= 499 => GlimmerTier.High,
                >= 500 and <= 899 => GlimmerTier.Dangerous,
                _ => GlimmerTier.Critical,
            };
        }
    }

    [Serializable, NetSerializable]
    public enum GlimmerTier : byte
    {
        Minimal,
        Low,
        Moderate,
        High,
        Dangerous,
        Critical,
    }
}
