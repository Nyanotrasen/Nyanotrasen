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
        public const float MinimumGlimmerForEvents = 100;

        public float NextEventAccumulator = 0;
        public float NextEventTime = 0;
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
            if (_sharedGlimmerSystem.Glimmer > MinimumGlimmerForEvents)
            {
                NextEventAccumulator += frameTime;
                if (NextEventAccumulator > NextEventTime)
                {
                    NextEventTime = _robustRandom.NextFloat(300, 1200);
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            NextEventTime = _robustRandom.NextFloat(300, 1200);
        }
    }
}
