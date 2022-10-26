using Content.Shared.Chemistry.Reagent;
using Content.Server.Abilities.Psionics;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Rerolls psionics once.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemRemovePsionic : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            var psySys = args.EntityManager.EntitySysManager.GetEntitySystem<PsionicAbilitiesSystem>();

            psySys.RemovePsionics(args.SolutionEntity);
        }
    }
}
