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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PacificationPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PacificationPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PacificationPowerComponent, PacificationPowerActionEvent>(OnPowerUsed);
        }

        private void OnInit(EntityUid uid, PacificationPowerComponent component, ComponentInit args)
        {
            if (_prototypeManager.TryIndex<EntityTargetActionPrototype>("Pacify", out var pacify))
                _actions.AddAction(uid, new EntityTargetAction(pacify), null);
        }

        private void OnShutdown(EntityUid uid, PacificationPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<EntityTargetActionPrototype>("Pacify", out var pacify))
                _actions.RemoveAction(uid, new EntityTargetAction(pacify), null);
        }

        private void OnPowerUsed(EntityUid uid, PacificationPowerComponent component, PacificationPowerActionEvent args)
        {
            if (_statusEffects.TryAddStatusEffect(args.Target, "Pacified", component.PacifyTime, false, "Pacified"))
                args.Handled = true;
        }
    }

    public sealed class PacificationPowerActionEvent : EntityTargetActionEvent {}
}