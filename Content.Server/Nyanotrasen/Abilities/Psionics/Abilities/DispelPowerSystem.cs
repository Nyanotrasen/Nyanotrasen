using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.StatusEffect;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Server.Guardian;
using Content.Server.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Server.Abilities.Psionics
{
    public sealed class DispelPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly GuardianSystem _guardianSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DispelPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DispelPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DispelPowerActionEvent>(OnPowerUsed);
        }

        private void OnInit(EntityUid uid, DispelPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<EntityTargetActionPrototype>("Dispel", out var pacify))
                return;

            component.DispelPowerAction = new EntityTargetAction(pacify);
            _actions.AddAction(uid, component.DispelPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic))
                psionic.PsionicAbility = component.DispelPowerAction;
        }

        private void OnShutdown(EntityUid uid, DispelPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<EntityTargetActionPrototype>("Dispel", out var pacify))
                _actions.RemoveAction(uid, new EntityTargetAction(pacify), null);
        }

        private void OnPowerUsed(DispelPowerActionEvent args)
        {
            if (!HasComp<DispellableComponent>(args.Target))
                return;

            if (TryComp<GuardianComponent>(args.Target, out var guardian))
            {
                DamageSpecifier damage = new();
                damage.DamageDict.Add("Blunt", 100);
                if (TryComp<GuardianHostComponent>(guardian.Host, out var host))
                    _guardianSystem.ToggleGuardian(host);

                _damageableSystem.TryChangeDamage(args.Target, damage, true, true);
                return;
            }

            EntityManager.QueueDeleteEntity(args.Target);
            Spawn("Ash", Transform(args.Target).Coordinates);
            _popupSystem.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", args.Target)), args.Target,
                Filter.Pvs(args.Target), PopupType.LargeCaution);
        }
    }

    public sealed class DispelPowerActionEvent : EntityTargetActionEvent {}
}
