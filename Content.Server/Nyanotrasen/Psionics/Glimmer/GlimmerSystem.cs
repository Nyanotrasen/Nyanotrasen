using System.Linq;
using Content.Shared.Psionics.Glimmer;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Psionics.Glimmer
{
    public sealed class GlimmerSystem : GameRuleSystem
    {
        public override string Prototype => "GlimmerEventRunner";
        [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public float DecayAccumulator = 0;
        public const float SecondsToLoseOneGlimmer = 10f;
        public const float MinimumGlimmerForEvents = 100;

        public override void Started() { }
        public override void Ended() { }

        public float NextEventAccumulator = 0;
        public float NextEventTime = 0;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            DecayAccumulator += frameTime;

            if (DecayAccumulator > SecondsToLoseOneGlimmer)
            {
                if (_robustRandom.Prob(0.75f))
                    _sharedGlimmerSystem.Glimmer--;
                DecayAccumulator -= SecondsToLoseOneGlimmer;
            }
            if (_sharedGlimmerSystem.Glimmer > MinimumGlimmerForEvents)
            {
                NextEventAccumulator += frameTime;
                if (NextEventAccumulator > NextEventTime)
                {
                    NextEventTime = _robustRandom.NextFloat(300, 900);
                    NextEventTime -= (float) (_sharedGlimmerSystem.Glimmer / 5);
                    NextEventAccumulator = 0;
                    RunGlimmerEvent();
                }
            }
        }

        private void RunGlimmerEvent()
        {
            var ev = GetGlimmerEvent();

            if (ev == null || !_prototypeManager.TryIndex<GameRulePrototype>(ev.Id, out var proto))
                return;

            GameTicker.StartGameRule(proto);
        }

        private GlimmerEventRuleConfiguration? GetGlimmerEvent()
        {
            var allEvents = _prototypeManager.EnumeratePrototypes<GameRulePrototype>()
                .Where(p => p.Configuration is GlimmerEventRuleConfiguration)
                .Select(p => (GlimmerEventRuleConfiguration) p.Configuration);

            var validEvents = new List<GlimmerEventRuleConfiguration>();

            foreach (var ev in allEvents)
            {
                if (ev == null)
                    continue;

                if (CanRun(ev))
                    validEvents.Add(ev);
            }

            if (validEvents.Count == 0)
                return null;

            return _robustRandom.Pick(validEvents);
        }

        private bool CanRun(GlimmerEventRuleConfiguration config)
        {
            if (_sharedGlimmerSystem.Glimmer < config.MinimumGlimmer)
                return false;

            if (_sharedGlimmerSystem.Glimmer > config.MaximumGlimmer)
                return false;

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();
            NextEventTime = _robustRandom.NextFloat(300, 900);
        }
    }
}
