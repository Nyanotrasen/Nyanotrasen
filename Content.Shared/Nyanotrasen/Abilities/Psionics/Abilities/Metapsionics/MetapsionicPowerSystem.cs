using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.StatusEffect;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Shared.Abilities.Psionics
{
    public sealed class MetapsionicPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedPopupSystem _popups = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MetapsionicPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<MetapsionicPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<MetapsionicPowerComponent, MetapsionicPowerActionEvent>(OnPowerUsed);
        }

        private void OnInit(EntityUid uid, MetapsionicPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<InstantActionPrototype>("MetapsionicPulse", out var metapsionicPulse))
                return;

            component.MetapsionicPowerAction = new InstantAction(metapsionicPulse);
            _actions.AddAction(uid, component.MetapsionicPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic))
                psionic.PsionicAbility = component.MetapsionicPowerAction;
        }

        private void OnShutdown(EntityUid uid, MetapsionicPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("MetapsionicPulse", out var metapsionicPulse))
                _actions.RemoveAction(uid, new InstantAction(metapsionicPulse), null);
        }

        private void OnPowerUsed(EntityUid uid, MetapsionicPowerComponent component, MetapsionicPowerActionEvent args)
        {
            foreach (var entity in _lookup.GetEntitiesInRange(uid, component.Range))
            {
                if (HasComp<PsionicComponent>(entity) && entity != uid && !HasComp<PsionicInsulationComponent>(entity))
                {
                    _popups.PopupEntity(Loc.GetString("metapsionic-pulse-success"), uid, Filter.Entities(uid), PopupType.LargeCaution);
                    args.Handled = true;
                    return;
                }
            }
            _popups.PopupEntity(Loc.GetString("metapsionic-pulse-failure"), uid, Filter.Entities(uid), PopupType.Large);
            _psionics.LogPowerUsed(uid, "metapsionic pulse");

            args.Handled = true;
        }
    }

    public sealed class MetapsionicPowerActionEvent : InstantActionEvent {}
}