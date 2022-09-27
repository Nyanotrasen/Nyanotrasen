using Robust.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;

namespace Content.Server.Abilities.Psionics
{
    public sealed class PsionicRegenerationPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicRegenerationPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PsionicRegenerationPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PsionicRegenerationPowerComponent, PsionicRegenerationPowerActionEvent>(OnPowerUsed);

            SubscribeLocalEvent<PsionicRegenerationPowerComponent, DispelledEvent>(OnDispelled);
        }

        private void OnInit(EntityUid uid, PsionicRegenerationPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<InstantActionPrototype>("Psionic Regeneration", out var metapsionic))
                return;

            component.PsionicRegenerationPowerAction = new InstantAction(metapsionic);
            _actions.AddAction(uid, component.PsionicRegenerationPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic) && psionic.PsionicAbility == null)
                psionic.PsionicAbility = component.PsionicRegenerationPowerAction;

            _tagSystem.TryAddTag(uid, "PsionicRegenerator");
        }

        private void OnPowerUsed(EntityUid uid, PsionicRegenerationPowerComponent component, PsionicRegenerationPowerActionEvent args)
        {
            if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
            {
                // This can (unintentionally) be drawn out by syringes and
                // hacked up by Felinid hairballs. If an in-character
                // explanation is needed for that, it could be said this
                // reagent represents some compound produced in the body by
                // precise psychic manipulation of the endocrine system.
                //
                // Some scifi nonsense like that.

                var solution = new Solution();
                solution.AddReagent("PsionicRegenerationEssence", FixedPoint2.New(20));
                _bloodstreamSystem.TryAddToChemicals(uid, solution, bloodstream);
                _audioSystem.PlayPvs(component.SoundUse, component.Owner, AudioParams.Default.WithVolume(8f).WithMaxDistance(1.5f).WithRolloffFactor(3.5f));
            }
            args.Handled = true;
        }

        private void OnShutdown(EntityUid uid, PsionicRegenerationPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("Psionic Regeneration", out var metapsionic))
                _actions.RemoveAction(uid, new InstantAction(metapsionic), null);

            _tagSystem.RemoveTag(uid, "PsionicRegenerator");
        }

        private void OnDispelled(EntityUid uid, PsionicRegenerationPowerComponent component, DispelledEvent args)
        {
            if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
            {
                // Dispellable reagents!
                _solutionSystem.TryRemoveReagent(uid, bloodstream.ChemicalSolution, "PsionicRegenerationEssence", FixedPoint2.MaxValue);
            }
            args.Handled = true;
        }
    }

    public sealed class PsionicRegenerationPowerActionEvent : InstantActionEvent {}
}

