using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Radio.Components;
using Content.Server.DoAfter;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Actions;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Tag;
using Robust.Shared.Timing;

namespace Content.Server.Abilities.Psionics
{
    public sealed class RFSensitivityPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RFSensitivityPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<RFSensitivityPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<RFSensitivityPowerComponent, RFSensitivityPowerActionEvent>(OnPowerUsed);

            SubscribeLocalEvent<RFSensitivityPowerComponent, DispelledEvent>(OnDispelled);
        }

        private void OnInit(EntityUid uid, RFSensitivityPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<InstantActionPrototype>("RFSensitivity", out var rfAction))
                return;

            component.RFSensitivityPowerAction = new InstantAction(rfAction);
            if (rfAction.UseDelay != null)
                component.RFSensitivityPowerAction.Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + (TimeSpan) rfAction.UseDelay);
            _actions.AddAction(uid, component.RFSensitivityPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic) && psionic.PsionicAbility == null)
                psionic.PsionicAbility = component.RFSensitivityPowerAction;


            var intrinsicRadio = EnsureComp<IntrinsicRadioReceiverComponent>(uid);

            AddRadio(uid);
        }

        private void OnPowerUsed(EntityUid uid, RFSensitivityPowerComponent component, RFSensitivityPowerActionEvent args)
        {
            if (HasComp<ActiveRadioComponent>(uid))
            {
                RemComp<ActiveRadioComponent>(uid);
                args.Handled = true;
            } else
            {
                AddRadio(uid);
                args.Handled = true;
            }
        }

        private void OnShutdown(EntityUid uid, RFSensitivityPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("RFSensitivity", out var rfAction))
                _actions.RemoveAction(uid, new InstantAction(rfAction), null);

            RemComp<IntrinsicRadioReceiverComponent>(uid);
            RemComp<ActiveRadioComponent>(uid);
        }

        private void OnDispelled(EntityUid uid, RFSensitivityPowerComponent component, DispelledEvent args)
        {
            if (component.RFSensitivityPowerAction == null)
                return;

            RemComp<ActiveRadioComponent>(uid);
            component.RFSensitivityPowerAction.Cooldown = (_gameTiming.CurTime, _gameTiming.CurTime + TimeSpan.FromMinutes(3));

            args.Handled = true;
        }


        private void AddRadio(EntityUid uid)
        {
            var activeRadio = EnsureComp<ActiveRadioComponent>(uid);

            var channels = _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>();

            foreach (var channel in channels)
            {
                if (channel.ID != "Binary")
                    activeRadio.Channels.Add(channel.ID);
            }
        }
    }

    public sealed class RFSensitivityPowerActionEvent : InstantActionEvent {}
}

