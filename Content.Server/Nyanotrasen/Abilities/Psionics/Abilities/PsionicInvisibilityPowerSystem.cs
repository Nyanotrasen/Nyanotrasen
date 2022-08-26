using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.StatusEffect;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Damage;
using Content.Shared.Stunnable;
using Content.Server.Psionics;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Audio;

namespace Content.Server.Abilities.Psionics
{
    public sealed class PsionicInvisibilityPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly PsionicInvisibilitySystem _invisibilitySystem = default!;
        [Dependency] private readonly SharedStunSystem _stunSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicInvisibilityPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PsionicInvisibilityPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PsionicInvisibilityPowerComponent, PsionicInvisibilityPowerAction>(OnPowerUsed);
            SubscribeLocalEvent<PsionicInvisibilityPowerOffAction>(OnPowerOff);
            SubscribeLocalEvent<PsionicInvisibilityUsedComponent, ComponentInit>(OnStart);
            SubscribeLocalEvent<PsionicInvisibilityUsedComponent, ComponentShutdown>(OnEnd);
            SubscribeLocalEvent<PsionicInvisibilityUsedComponent, DamageChangedEvent>(OnDamageChanged);
        }

        private void OnInit(EntityUid uid, PsionicInvisibilityPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<InstantActionPrototype>("PsionicInvisibility", out var invis))
                return;

            component.PsionicInvisibilityPowerAction = new InstantAction(invis);
            _actions.AddAction(uid, component.PsionicInvisibilityPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic))
                psionic.PsionicAbility = component.PsionicInvisibilityPowerAction;
        }

        private void OnShutdown(EntityUid uid, PsionicInvisibilityPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("PsionicInvisibility", out var invis))
                _actions.RemoveAction(uid, new InstantAction(invis), null);
        }

        private void OnPowerUsed(EntityUid uid, PsionicInvisibilityPowerComponent component, PsionicInvisibilityPowerAction args)
        {
            if (HasComp<PsionicInvisibilityUsedComponent>(uid))
                return;

            ToggleInvisibility(args.Performer);

            if (_prototypeManager.TryIndex<InstantActionPrototype>("PsionicInvisibilityOff", out var invis))
                _actions.AddAction(args.Performer, new InstantAction(invis), null);

            args.Handled = true;
        }

        private void OnPowerOff(PsionicInvisibilityPowerOffAction args)
        {
            if (!HasComp<PsionicInvisibilityUsedComponent>(args.Performer))
                return;

            ToggleInvisibility(args.Performer);
            args.Handled = true;
        }

        private void OnStart(EntityUid uid, PsionicInvisibilityUsedComponent component, ComponentInit args)
        {
            EnsureComp<PsionicallyInvisibleComponent>(uid);
            EnsureComp<PacifiedComponent>(uid);
            SoundSystem.Play("/Audio/Effects/toss.ogg", Filter.Pvs(uid), uid);

        }

        private void OnEnd(EntityUid uid, PsionicInvisibilityUsedComponent component, ComponentShutdown args)
        {
            if (Terminating(uid))
                return;

            RemComp<PsionicallyInvisibleComponent>(uid);
            RemComp<PacifiedComponent>(uid);
            SoundSystem.Play("/Audio/Effects/toss.ogg", Filter.Pvs(uid), uid);

            if (_prototypeManager.TryIndex<InstantActionPrototype>("PsionicInvisibilityOff", out var invis))
                _actions.RemoveAction(uid, new InstantAction(invis), null);

            _stunSystem.TryParalyze(uid, TimeSpan.FromSeconds(8), false);
        }

        private void OnDamageChanged(EntityUid uid, PsionicInvisibilityUsedComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased)
                return;

            ToggleInvisibility(uid);
        }

        public void ToggleInvisibility(EntityUid uid)
        {
            if (!HasComp<PsionicInvisibilityUsedComponent>(uid))
            {
                EnsureComp<PsionicInvisibilityUsedComponent>(uid);
            } else
            {
                _invisibilitySystem.SetCanSeePsionicInvisiblity(uid, false);
                RemCompDeferred<PsionicInvisibilityUsedComponent>(uid);
            }
        }
    }

    public sealed class PsionicInvisibilityPowerAction : InstantActionEvent {}
    public sealed class PsionicInvisibilityPowerOffAction : InstantActionEvent {}
}
