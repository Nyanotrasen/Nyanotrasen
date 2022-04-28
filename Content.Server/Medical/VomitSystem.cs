using Content.Server.Nutrition.Components;
using Content.Server.Stunnable;
using Content.Server.Body.Components;
using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Medical
{
    public sealed class VomitSystem : EntitySystem
    {

        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;

        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public void Vomit(EntityUid uid, float thirstAdded = -15f, float hungerAdded = -15f)
        {
            if (TryComp<HungerComponent>(uid, out var hunger))
                hunger.UpdateFood(hungerAdded);

            if (TryComp<ThirstComponent>(uid, out var thirst))
                thirst.UpdateThirst(thirstAdded);

            float solutionSize = (Math.Abs(thirstAdded) + Math.Abs(hungerAdded)) / 4;

            if (TryComp<StatusEffectsComponent>(uid, out var status))
                _stunSystem.TrySlowdown(uid, TimeSpan.FromSeconds(solutionSize), true, 0.5f, 0.5f, status);

            var puddle = EntityManager.SpawnEntity("PuddleVomit", Transform(uid).Coordinates);

            var puddleComp = Comp<PuddleComponent>(puddle);

            SoundSystem.Play(Filter.Pvs(uid), "/Audio/Effects/Diseases/vomiting.ogg", AudioHelpers.WithVariation(0.2f));

            if (TryComp<BloodstreamComponent>(uid, out var bloodStream))
            {
                var temp = bloodStream.ChemicalSolution.SplitSolution(solutionSize);
                if (_solutionSystem.TryGetSolution(puddle, puddleComp.SolutionName, out var puddleSolution))
                {
                    _solutionSystem.TryAddSolution(puddle, puddleSolution, temp);
                }
            }
        }
    }
}
