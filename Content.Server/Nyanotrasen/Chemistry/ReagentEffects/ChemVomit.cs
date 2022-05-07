using Content.Shared.Chemistry.Reagent;
using Content.Server.Medical;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Forces you to vomit.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemVomit : ReagentEffect
    {
        [DataField("thirstAmount")]
        public float ThirstAmount = -40f;

        [DataField("hungerAmount")]
        public float HungerAmount = -40f;

        public override void Effect(ReagentEffectArgs args)
        {
            var vomitSys = args.EntityManager.EntitySysManager.GetEntitySystem<VomitSystem>();

            vomitSys.Vomit(args.SolutionEntity, ThirstAmount, HungerAmount);
        }
    }
}
