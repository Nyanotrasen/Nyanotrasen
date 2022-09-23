using Content.Shared.Abilities.Psionics;
using Content.Shared.Radio;
using Content.Server.Research.SophicScribe;
using Content.Server.Ghost.Components;
using Content.Server.Radio.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Psionics
{
    public sealed class GlimmerSystem : EntitySystem
    {
        [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public float DecayAccumulator = 0;
        public float GlimmerAnnounceAccumulator = 0;
        public const float SecondsToLoseOneGlimmer = 10f;
        public const float GlimmerAnnounceIntervalSeconds = 120f;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            DecayAccumulator += frameTime;
            GlimmerAnnounceAccumulator += frameTime;

            if (DecayAccumulator > SecondsToLoseOneGlimmer)
            {
                if (_robustRandom.Prob(0.5f))
                    _sharedGlimmerSystem.AddToGlimmer(-1);
                DecayAccumulator -= SecondsToLoseOneGlimmer;
            }

            if (GlimmerAnnounceAccumulator > GlimmerAnnounceIntervalSeconds)
            {
                GlimmerAnnounceAccumulator -= GlimmerAnnounceIntervalSeconds;
                if (_sharedGlimmerSystem.Glimmer == 0)
                    return;
                foreach (var scribe in EntityQuery<SophicScribeComponent>())
                {
                    if (!TryComp<IntrinsicRadioComponent>(scribe.Owner, out var radio)) return;

                    var message = Loc.GetString("glimmer-report", ("level", _sharedGlimmerSystem.Glimmer));
                    var channel = _prototypeManager.Index<RadioChannelPrototype>("Science");
                    _radioSystem.SpreadMessage(radio, scribe.Owner, message, channel);
                }
            }
        }

    }
}
