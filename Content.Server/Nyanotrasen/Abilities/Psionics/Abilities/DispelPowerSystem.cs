using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.StatusEffect;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Damage;
using Content.Server.Guardian;
using Content.Server.Popups;
using Content.Server.Bible.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Abilities.Psionics
{
    public sealed class DispelPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly GuardianSystem _guardianSystem = default!;
        [Dependency] private readonly MindSwapPowerSystem _mindSwap = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DispelPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DispelPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DispelPowerActionEvent>(OnPowerUsed);

            // Upstream stuff we're just gonna handle here
            SubscribeLocalEvent<DispellableComponent, DispelledEvent>(OnDispelled);
            SubscribeLocalEvent<GuardianComponent, DispelledEvent>(OnGuardianDispelled);
            SubscribeLocalEvent<FamiliarComponent, DispelledEvent>(OnFamiliarDispelled);
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
            if (HasComp<PsionicInsulationComponent>(args.Target))
                return;

            var ev = new DispelledEvent();
            RaiseLocalEvent(args.Target, ev, false);

            args.Handled = ev.Handled;
        }

        private void OnDispelled(EntityUid uid, DispellableComponent component, DispelledEvent args)
        {
            QueueDel(uid);
            Spawn("Ash", Transform(uid).Coordinates);
            args.Handled = true;
        }

        private void OnGuardianDispelled(EntityUid uid, GuardianComponent guardian, DispelledEvent args)
        {
            DamageSpecifier damage = new();
            damage.DamageDict.Add("Blunt", 100);
            if (TryComp<GuardianHostComponent>(guardian.Host, out var host))
                _guardianSystem.ToggleGuardian(host);

            _damageableSystem.TryChangeDamage(uid, damage, true, true);
            args.Handled = true;
        }

        private void OnFamiliarDispelled(EntityUid uid, FamiliarComponent component, DispelledEvent args)
        {
            if (component.Source != null)
                EnsureComp<SummonableRespawningComponent>(component.Source.Value);

            args.Handled = true;
        }
    }

    public sealed class DispelPowerActionEvent : EntityTargetActionEvent {}

    public sealed class DispelledEvent : HandledEntityEventArgs {}
}
