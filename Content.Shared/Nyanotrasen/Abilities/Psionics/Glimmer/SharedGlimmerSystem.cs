using Content.Shared.GameTicking;

namespace Content.Shared.Abilities.Psionics
{
    /// <summary>
    /// How much psionic power is flowing around.
    /// 1 average power use = 10 glimmer.
    /// Passively lose 1 glimmer every 10 seconds.
    /// </summary>
    public sealed class SharedGlimmerSystem : EntitySystem
    {
        // TODO: Should this be per station? I can see arguments either way.
        [Access(typeof(SharedGlimmerSystem))]
        public int Glimmer = 0;
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
        /// Add 'toAdd' to the glimmer value.
        /// You can subtract using a negative value.
        /// </summary>
        public void AddToGlimmer(int toAdd)
        {
            Glimmer += toAdd;
            Glimmer = Math.Clamp(Glimmer, 0, 1000);
        }
    }
}
