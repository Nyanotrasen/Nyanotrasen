using Content.Server.Instruments;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Events;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Traitor.Uplink.Components;
using Content.Server.PDA.Ringer;
using Content.Server.UserInterface;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.PDA
{
    public sealed class PDASystem : SharedPDASystem
    {
        [Dependency] private readonly UplinkSystem _uplinkSystem = default!;
        [Dependency] private readonly UplinkAccountsSystem _uplinkAccounts = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;
        [Dependency] private readonly RingerSystem _ringerSystem = default!;
        [Dependency] private readonly InstrumentSystem _instrumentSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PDAComponent, LightToggleEvent>(OnLightToggle);
            SubscribeLocalEvent<PDAComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);
            SubscribeLocalEvent<PDAComponent, UplinkInitEvent>(OnUplinkInit);
            SubscribeLocalEvent<PDAComponent, UplinkRemovedEvent>(OnUplinkRemoved);
        }

        protected override void OnComponentInit(EntityUid uid, PDAComponent pda, ComponentInit args)
        {
            base.OnComponentInit(uid, pda, args);

            if (!TryComp(uid, out ServerUserInterfaceComponent? uiComponent))
                return;

            if (_uiSystem.TryGetUi(uid, PDAUiKey.Key, out var ui, uiComponent))
                ui.OnReceiveMessage += (msg) => OnUIMessage(pda, msg);
        }

        protected override void OnItemInserted(EntityUid uid, PDAComponent pda, EntInsertedIntoContainerMessage args)
        {
            base.OnItemInserted(uid, pda, args);
            UpdatePDAUserInterface(pda);
        }

        protected override void OnItemRemoved(EntityUid uid, PDAComponent pda, EntRemovedFromContainerMessage args)
        {
            base.OnItemRemoved(uid, pda, args);
            UpdatePDAUserInterface(pda);
        }

        private void OnLightToggle(EntityUid uid, PDAComponent pda, LightToggleEvent args)
        {
            pda.FlashlightOn = args.IsOn;
            UpdatePDAUserInterface(pda);
        }

        public void SetOwner(PDAComponent pda, string ownerName)
        {
            pda.OwnerName = ownerName;
            UpdatePDAUserInterface(pda);
        }

        private void OnUplinkInit(EntityUid uid, PDAComponent pda, UplinkInitEvent args)
        {
            UpdatePDAUserInterface(pda);
        }

        private void OnUplinkRemoved(EntityUid uid, PDAComponent pda, UplinkRemovedEvent args)
        {
            UpdatePDAUserInterface(pda);
        }

        private void UpdatePDAUserInterface(PDAComponent pda)
        {
            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = pda.OwnerName,
                IdOwner = pda.ContainedID?.FullName,
                JobTitle = pda.ContainedID?.JobTitle
            };

            if (!_uiSystem.TryGetUi(pda.Owner, PDAUiKey.Key, out var ui))
                return;

            var hasInstrument = HasComp<InstrumentComponent>(pda.Owner);
            var state = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, false, hasInstrument);

            ui.SetState(state);

            // TODO UPLINK RINGTONES/SECRETS This is just a janky placeholder way of hiding uplinks from non syndicate
            // players. This should really use a sort of key-code entry system that selects an account which is not directly tied to
            // a player entity.

            if (!HasComp<UplinkComponent>(pda.Owner))
                return;

            var uplinkState = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, true, hasInstrument);

            foreach (var session in ui.SubscribedSessions)
            {
                if (session.AttachedEntity is not EntityUid { Valid: true } user)
                    continue;

                if (_uplinkAccounts.HasAccount(user))
                    ui.SetState(uplinkState, session);
            }
        }

        private void OnUIMessage(PDAComponent pda, ServerBoundUserInterfaceMessage msg)
        {
            // todo: move this to entity events
            switch (msg.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                    UpdatePDAUserInterface(pda);
                    break;
                case PDAToggleFlashlightMessage _:
                    {
                        if (EntityManager.TryGetComponent(pda.Owner, out UnpoweredFlashlightComponent? flashlight))
                            _unpoweredFlashlight.ToggleLight(flashlight);
                        break;
                    }

                case PDAShowUplinkMessage _:
                    {
                        if (EntityManager.TryGetComponent(pda.Owner, out UplinkComponent? uplink))
                            _uplinkSystem.ToggleUplinkUI(uplink, msg.Session);
                        break;
                    }
                case PDAShowRingtoneMessage _:
                    {
                        if (EntityManager.TryGetComponent(pda.Owner, out RingerComponent? ringer))
                            _ringerSystem.ToggleRingerUI(ringer, msg.Session);
                        break;
                    }
                case PDAShowMusicMessage _:
                {
                    if (TryComp(pda.Owner, out InstrumentComponent? instrument))
                        _instrumentSystem.ToggleInstrumentUi(pda.Owner, msg.Session, instrument);
                    break;
                }
            }
        }

        private void AfterUIOpen(EntityUid uid, PDAComponent pda, AfterActivatableUIOpenEvent args)
        {
            // A new user opened the UI --> Check if they are a traitor and should get a user specific UI state override.
            if (!HasComp<UplinkComponent>(pda.Owner) || !_uplinkAccounts.HasAccount(args.User))
                return;

            if (!_uiSystem.TryGetUi(pda.Owner, PDAUiKey.Key, out var ui))
                return;

            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = pda.OwnerName,
                IdOwner = pda.ContainedID?.FullName,
                JobTitle = pda.ContainedID?.JobTitle
            };

            var state = new PDAUpdateState(pda.FlashlightOn, pda.PenSlot.HasItem, ownerInfo, true, HasComp<InstrumentComponent>(pda.Owner));

            ui.SetState(state, args.Session);
        }
    }
}
