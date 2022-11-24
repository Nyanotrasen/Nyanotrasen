using Content.Shared.Chemistry.Reagent;
using Content.Server.Psionics;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Rerolls psionics once.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemRerollPsionic : ReagentEffect
    {
        /// <summary>
        /// Reroll multiplier.
        /// </summary>
        [DataField("bonusMultiplier")]
        public float BonusMuliplier = 1f;

        public override void Effect(ReagentEffectArgs args)
        {
            var psySys = args.EntityManager.EntitySysManager.GetEntitySystem<PsionicsSystem>();

            psySys.RerollPsionics(args.SolutionEntity, bonusMuliplier: BonusMuliplier);
        }
    }
}
