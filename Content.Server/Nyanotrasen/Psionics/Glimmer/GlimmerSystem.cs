using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Random;

namespace Content.Server.Psionics.Glimmer
{
    public sealed class GlimmerSystem : EntitySystem
    {
        [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public float DecayAccumulator = 0;
        public const float SecondsToLoseOneGlimmer = 10f;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            DecayAccumulator += frameTime;

            if (DecayAccumulator > SecondsToLoseOneGlimmer)
            {
                if (_robustRandom.Prob(0.5f))
                    _sharedGlimmerSystem.AddToGlimmer(-1);
                DecayAccumulator -= SecondsToLoseOneGlimmer;
            }
        }
    }
}
