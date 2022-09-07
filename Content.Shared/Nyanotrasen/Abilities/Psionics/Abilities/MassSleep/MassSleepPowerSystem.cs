using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Abilities.Psionics
{
    public sealed class MassSleepPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MassSleepPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<MassSleepPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<MassSleepPowerComponent, MassSleepPowerActionEvent>(OnPowerUsed);
        }

        private void OnInit(EntityUid uid, MassSleepPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<WorldTargetActionPrototype>("MassSleep", out var massSleep))
                return;

            component.MassSleepPowerAction = new WorldTargetAction(massSleep);
            _actions.AddAction(uid, component.MassSleepPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic))
                psionic.PsionicAbility = component.MassSleepPowerAction;
        }

        private void OnShutdown(EntityUid uid, MassSleepPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<WorldTargetActionPrototype>("MassSleep", out var massSleep))
                _actions.RemoveAction(uid, new WorldTargetAction(massSleep), null);
        }

        private void OnPowerUsed(EntityUid uid, MassSleepPowerComponent component, MassSleepPowerActionEvent args)
        {
            foreach (var entity in _lookup.GetEntitiesInRange(args.Target, component.Radius))
            {
                if (HasComp<MobStateComponent>(entity) && entity != uid && !HasComp<PsionicInsulationComponent>(entity))
                {
                    if (TryComp<DamageableComponent>(entity, out var damageable) && damageable.DamageContainerID == "Biological")
                        EnsureComp<SleepingComponent>(entity);
                }
            }
            _psionics.LogPowerUsed(uid, "Mass Sleep");
            args.Handled = true;
        }
    }

    public sealed class MassSleepPowerActionEvent : WorldTargetActionEvent {}
}