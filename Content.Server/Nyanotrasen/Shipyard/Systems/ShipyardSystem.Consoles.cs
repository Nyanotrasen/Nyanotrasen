using Content.Server.Popups;
using Content.Server.Cargo.Systems;
using Content.Server.Shipyard.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Shipyard.Events;
using Content.Shared.Shipyard.BUI;
using Content.Shared.Shipyard.Prototypes;
using Content.Shared.Shipyard.Components;
using Content.Server.Cargo.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Shipyard;

namespace Content.Server.Shipyard.Systems
{
    public sealed class ShipyardConsoleSystem : SharedShipyardSystem
    {
        [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ShipyardSystem _shipyard = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly CargoSystem _cargo = default!;

        public void InitializeConsole()
        {
            SubscribeLocalEvent<ShipyardConsoleComponent, ShipyardConsolePurchaseMessage>(OnPurchaseMessage);
            //SubscribeLocalEvent<ShipyardConsoleComponent, ShipyardConsoleSellMessage>(OnSellMessage);
            SubscribeLocalEvent<ShipyardConsoleComponent, BoundUIOpenedEvent>(OnConsoleUIOpened);
            //SubscribeLocalEvent<ShipyardConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

        private void OnInit(EntityUid uid, SharedShipyardConsoleComponent orderConsole, ComponentInit args)
        {
            //_shipyard.SetupShipyard(); ///if we have to start up the shipyard from here later
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            //_shipyard.Shutdown(); //round cleanup event in case of needing OnInit;
        }

        public void OnPurchaseMessage(EntityUid uid, SharedShipyardConsoleComponent component, ShipyardConsolePurchaseMessage args)
        {
            if (args.Session.AttachedEntity is not { Valid : true } player)
            {
                return;
            }

            if (TryComp<AccessReaderComponent>(uid, out var accessReaderComponent) && accessReaderComponent.Enabled && !_accessSystem.IsAllowed(player, accessReaderComponent))
            {
                ConsolePopup(args.Session, Loc.GetString("comms-console-permission-denied"));
                PlayDenySound(uid, component);
                return;
            }

            if (args.Price <= 0)
                return;

            var station = _station.GetOwningStation(uid);
            var bank = GetBankAccount(station);

            if (bank == null)
                return;

            if (bank.Balance <= args.Price)
            {
                ConsolePopup(args.Session, Loc.GetString("cargo-console-insufficient-funds", ("cost", args.Price)));
                PlayDenySound(uid, component);
                return;
            }

            if (!_prototypeManager.TryIndex<VesselPrototype>(args.Vessel, out var vessel) || vessel == null)
            {
                ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-vessel", ("vessel", args.Vessel)));
                PlayDenySound(uid, component);
                return;
            }

            if (!TryPurchaseVessel(bank, vessel, out var shuttle) || shuttle == null)
            {
                PlayDenySound(uid, component);
                return;
            }

            _cargo.DeductFunds(bank, vessel.Price);
            PlayConfirmSound(uid, component);
            var newDeed = EnsureComp<ShuttleDeedComponent>(bank.Owner);

            var newState = new ShipyardConsoleInterfaceState(
                bank.Balance,
                true);

            _uiSystem.TrySetUiState(component.Owner, ShipyardConsoleUiKey.Shipyard, newState);
            RegisterDeed(newDeed, shuttle);           
        }

        public void OnSellMessage(EntityUid uid, SharedShipyardConsoleComponent component, ShipyardConsoleSellMessage args)
        {
            
            if (args.Session.AttachedEntity is not { Valid: true } player)
            {
                return;
            }

            var station = _station.GetOwningStation(uid);
            var bank = GetBankAccount(station);

            if (bank == null)
                return;

            if (!TryComp<ShuttleDeedComponent>(bank.Owner, out var deed) || deed == null || deed.ShuttleUid == null)
            {
                ConsolePopup(args.Session, Loc.GetString("shipyard-console-invalid-vessel"));
                PlayDenySound(uid, component);
                return;
            }

            if (!TrySellVessel(bank, deed.ShuttleUid, out var bill))
            {
                PlayDenySound(uid, component);
                return;
            };

            _cargo.DeductFunds(bank, -bill);
            PlayConfirmSound(uid, component);

            ShipyardConsoleInterfaceState newState = new ShipyardConsoleInterfaceState(
                    bank.Balance,
                    true);

            _uiSystem.TrySetUiState(component.Owner, ShipyardConsoleUiKey.Shipyard, newState);
        }

        private void OnConsoleUIOpened(EntityUid uid, SharedShipyardConsoleComponent component, BoundUIOpenedEvent args)
        {
            if (!args.Session.AttachedEntity.HasValue)
                return;

            var station = _station.GetOwningStation(component.Owner);
            var bank = GetBankAccount(station);

            if (bank == null)
                return;

            var newState = new ShipyardConsoleInterfaceState(
                bank.Balance,
                true);

            _uiSystem.TrySetUiState(component.Owner, ShipyardConsoleUiKey.Shipyard, newState);
        }

        private void ConsolePopup(ICommonSession session, string text)
        {
            if (session.AttachedEntity is { Valid : true } player)
                _popup.PopupEntity(text, player);
        }

        private void PlayDenySound(EntityUid uid, SharedShipyardConsoleComponent component)
        {
            SoundSystem.Play(component.ErrorSound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), uid);
        }
        private void PlayConfirmSound(EntityUid uid, SharedShipyardConsoleComponent component)
        {
            SoundSystem.Play(component.ConfirmSound.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), uid);
        }

        public bool TryPurchaseVessel(StationBankAccountComponent component, VesselPrototype vessel, out ShuttleComponent? deed)
        {
            var stationUid = _station.GetOwningStation(component.Owner);

            if (component == null || vessel == null || vessel.ShuttlePath == null || stationUid == null)
            {
                deed = null;
                return false;
            };

            _shipyard.PurchaseShuttle(stationUid, vessel.ShuttlePath.ToString(), out deed);

            if (deed == null)
            {
                return false;
            };

            return true;
        }
        public bool TrySellVessel(StationBankAccountComponent component, EntityUid? gridUid, out int bill)
        {
            bill = 0;
            var stationUid = _station.GetOwningStation(component.Owner);
            if (component == null || gridUid == null || stationUid == null)
            {
                return false;
            };

            _shipyard.SellShuttle((EntityUid) stationUid, (EntityUid) gridUid, out bill);

            if (bill == 0)
            {
                return false;
            };

            return true;
        }

        private void RegisterDeed(ShuttleDeedComponent deed, ShuttleComponent shuttle)
        {
            deed.ShuttleUid = shuttle.Owner;
            //Dirty(deed); //done dirt cheap
            //Since this is station based for now, client UIs dont need to know
        }

        
        //public void DeductFunds(OtherBankAccountComponent component, int amount)
        //{
        //    component.Balance = Math.Max(0, component.Balance - amount);
        //    Dirty(component);
        //    //In the case that this uses a separate income pool than generic cargo
        //}

        public StationBankAccountComponent? GetBankAccount(EntityUid? uid)
        {
            if (uid != null && TryComp<StationBankAccountComponent>(uid, out var bankAccount))
            { 
                return bankAccount;
            }    
            return null;
        }
    }
}
