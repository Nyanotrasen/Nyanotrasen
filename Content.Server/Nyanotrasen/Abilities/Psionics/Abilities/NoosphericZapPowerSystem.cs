using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Abilities.Psionics;
using Content.Server.Psionics;
using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Server.Stunnable;

namespace Content.Server.Abilities.Psionics
{
    public sealed class NoosphericZapPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NoosphericZapPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<NoosphericZapPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<NoosphericZapPowerActionEvent>(OnPowerUsed);
        }

        private void OnInit(EntityUid uid, NoosphericZapPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<EntityTargetActionPrototype>("NoosphericZap", out var noosphericZap))
                return;

            component.NoosphericZapPowerAction = new EntityTargetAction(noosphericZap);
            _actions.AddAction(uid, component.NoosphericZapPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic) && psionic.PsionicAbility == null)
                psionic.PsionicAbility = component.NoosphericZapPowerAction;
        }

        private void OnShutdown(EntityUid uid, NoosphericZapPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<EntityTargetActionPrototype>("NoosphericZap", out var noosphericZap))
                _actions.RemoveAction(uid, new EntityTargetAction(noosphericZap), null);
        }

        private void OnPowerUsed(NoosphericZapPowerActionEvent args)
        {
            if (!(HasComp<PotentialPsionicComponent>(args.Target)))
                return;

            if (HasComp<PsionicInsulationComponent>(args.Target))
                return;

            _stunSystem.TryParalyze(args.Target, TimeSpan.FromSeconds(5), false);
            _statusEffectsSystem.TryAddStatusEffect(args.Target, "Stutter", TimeSpan.FromSeconds(30), false, "StutteringAccent");

            _psionics.LogPowerUsed(args.Performer, "noospheric zap");
            args.Handled = true;
        }

    }

    public sealed class NoosphericZapPowerActionEvent : EntityTargetActionEvent {}
}
