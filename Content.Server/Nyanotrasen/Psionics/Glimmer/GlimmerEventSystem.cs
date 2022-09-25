using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules;
using Content.Shared.Database;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Psionics.Glimmer
{
    /// <summary>
    ///     An abstract entity system inherited by all station events for their behavior.
    /// </summary>
    public abstract class GlimmerEventSystem : GameRuleSystem
    {
        [Dependency] protected readonly IAdminLogManager AdminLogManager = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly IRobustRandom RobustRandom = default!;
        [Dependency] protected readonly SharedGlimmerSystem GlimmerSystem = default!;

        protected ISawmill Sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            Sawmill = Logger.GetSawmill("stationevents");
        }

        /// <summary>
        ///     Called once to setup the event after StartAfter has elapsed, or if an event is forcibly started.
        /// </summary>
        public override void Started()
        {
            AdminLogManager.Add(LogType.EventStarted, LogImpact.High, $"Event started: {Configuration.Id}");
        }

        /// <summary>
        ///     Called once as soon as an event is added, for announcements.
        ///     Can also be used for some initial setup.
        /// </summary>
        public override void Added()
        {
            AdminLogManager.Add(LogType.EventAnnounced, $"Event added / announced: {Configuration.Id}");

            if (Configuration is not GlimmerEventRuleConfiguration ev)
                return;
        }

        /// <summary>
        ///     Called once when the station event ends for any reason.
        /// </summary>
        public override void Ended()
        {
            AdminLogManager.Add(LogType.EventStopped, $"Event ended: {Configuration.Id}");

            if (Configuration is not GlimmerEventRuleConfiguration ev)
                return;

            GlimmerSystem.AddToGlimmer(0 - (RobustRandom.Next(ev.GlimmerBurn.Item1, ev.GlimmerBurn.Item2)));
        }

        #region Helper Functions

        protected void ForceEndSelf()
        {
            GameTicker.EndGameRule(PrototypeManager.Index<GameRulePrototype>(Prototype));
        }

        #endregion
    }
}
