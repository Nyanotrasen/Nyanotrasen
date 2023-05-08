using Content.Server.GameTicking.Rules.Components;
using Content.Server.Psionics.Glimmer;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.StationEvents.Events
{
    public sealed class GlimmerEventSystem : StationEventSystem<GlimmerEventComponent>
    {
        [Dependency] private readonly SharedGlimmerSystem GlimmerSystem = default!;

        protected override void Ended(EntityUid uid, GlimmerEventComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, component, gameRule, args);

            var glimmerBurned = RobustRandom.Next(component.GlimmerBurnLower, component.GlimmerBurnUpper);
            GlimmerSystem.Glimmer -= glimmerBurned;

            var reportEv = new GlimmerEventEndedEvent(component.SophicReport, glimmerBurned);
            RaiseLocalEvent(reportEv);
        }
    }

    public sealed class GlimmerEventEndedEvent : EntityEventArgs
    {
        public string Message = "";
        public int GlimmerBurned = 0;

        public GlimmerEventEndedEvent(string message, int glimmerBurned)
        {
            Message = message;
            GlimmerBurned = glimmerBurned;
        }
    }
}
