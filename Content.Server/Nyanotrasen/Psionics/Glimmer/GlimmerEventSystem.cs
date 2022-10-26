using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;


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
        [Dependency] protected readonly StationSystem _stationSystem = default!;

        /// <summary>
        ///     How long has the event existed. Do not change this.
        /// </summary>
        protected float Elapsed { get; set; }

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

            var glimmerBurned = RobustRandom.Next(ev.GlimmerBurnLower, ev.GlimmerBurnUpper);
            GlimmerSystem.Glimmer -= glimmerBurned;

            var reportEv = new GlimmerEventEndedEvent(ev.SohpicReport, glimmerBurned);
            RaiseLocalEvent(reportEv);
        }

        public override void Update(float frameTime)
        {
            if (!RuleAdded || Configuration is not GlimmerEventRuleConfiguration data)
                return;

            Elapsed += frameTime;
            if (Elapsed > 1f)
                GameTicker.EndGameRule(PrototypeManager.Index<GameRulePrototype>(Prototype));
        }

        #region Helper Functions

        protected void ForceEndSelf()
        {
            GameTicker.EndGameRule(PrototypeManager.Index<GameRulePrototype>(Prototype));
        }

        #endregion
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
