using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.Abilities.Psionics
{
    public sealed class PacificationPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PacificationPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PacificationPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PacificationPowerComponent, PacificationPowerActionEvent>(OnPowerUsed);
        }

        private void OnInit(EntityUid uid, PacificationPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<EntityTargetActionPrototype>("Pacify", out var pacify))
                return;

            component.PacificationPowerAction = new EntityTargetAction(pacify);
            _actions.AddAction(uid, component.PacificationPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic) && psionic.PsionicAbility == null)
                psionic.PsionicAbility = component.PacificationPowerAction;
        }

        private void OnShutdown(EntityUid uid, PacificationPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<EntityTargetActionPrototype>("Pacify", out var pacify))
                _actions.RemoveAction(uid, new EntityTargetAction(pacify), null);
        }

        private void OnPowerUsed(EntityUid uid, PacificationPowerComponent component, PacificationPowerActionEvent args)
        {
            if (HasComp<PsionicInsulationComponent>(args.Target))
                return;

            if (_statusEffects.TryAddStatusEffect(args.Target, "Pacified", component.PacifyTime, false, "Pacified"))
                args.Handled = true;

            _psionics.LogPowerUsed(uid, "pacification");
        }
    }

    public sealed class PacificationPowerActionEvent : EntityTargetActionEvent {}
}