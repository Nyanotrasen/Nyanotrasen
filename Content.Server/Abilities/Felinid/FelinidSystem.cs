using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.StatusEffect;
using Content.Server.Body.Components;
using Content.Server.Medical;
using Content.Server.Chemistry.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared.Throwing;
using Robust.Shared.Random;

namespace Content.Server.Abilities.Felinid
{
    public sealed class FelinidSystem : EntitySystem
    {

        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly VomitSystem _vomitSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FelinidComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<FelinidComponent, HairballActionEvent>(OnHairball);
            SubscribeLocalEvent<HairballComponent, ThrowDoHitEvent>(OnHairballHit);
        }

        private void OnInit(EntityUid uid, FelinidComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, component.HairballAction, uid);
        }

        private void OnHairball(EntityUid uid, FelinidComponent component, HairballActionEvent args)
        {
            SoundSystem.Play(Filter.Pvs(uid), "/Audio/Effects/Species/hairball.ogg", uid, AudioHelpers.WithVariation(0.15f));

            var hairball = EntityManager.SpawnEntity(component.HairballPrototype, Transform(uid).Coordinates);
            var hairballComp = Comp<HairballComponent>(hairball);

            if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
            {
                var temp = bloodstream.ChemicalSolution.SplitSolution(20);

                if (_solutionSystem.TryGetSolution(hairball, hairballComp.SolutionName, out var hairballSolution))
                {
                    _solutionSystem.TryAddSolution(hairball, hairballSolution, temp);
                }
            }
            args.Handled = true;
        }

        private void OnHairballHit(EntityUid uid, HairballComponent component, ThrowDoHitEvent args)
        {
            if (HasComp<FelinidComponent>(args.Target) || !HasComp<StatusEffectsComponent>(args.Target))
                return;
            if (_robustRandom.Prob(0.2f))
                _vomitSystem.Vomit(args.Target);
        }
    }

    public sealed class HairballActionEvent : InstantActionEvent {}
}
