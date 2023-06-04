using Content.Shared.Chemistry.Reagent;
using Content.Server.Disease;
using Content.Shared.Disease;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemCureDisease : ReagentEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-cure-disease", ("chance", Probability));

        /// <summary>
        /// Chance it has each tick to cure a disease, between 0 and 1
        /// </summary>
        [DataField("cureChance")]
        public float CureChance = 0.15f;

        public override void Effect(ReagentEffectArgs args)
        {
            var cureChance = CureChance;

            cureChance *= args.Scale;

            var ev = new CureDiseaseAttemptEvent(cureChance);
            args.EntityManager.EventBus.RaiseLocalEvent(args.SolutionEntity, ev, false);
        }
    }
}
