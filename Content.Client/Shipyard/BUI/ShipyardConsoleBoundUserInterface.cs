using Content.Client.Shipyard.UI;
using Content.Client.Shipyard.Components;
using Content.Shared.Shipyard.BUI;
using Content.Shared.Shipyard.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using static Content.Shared.Shipyard.Components.SharedShipyardConsoleComponent;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Shipyard.BUI
{
    public sealed class ShipyardConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables]
        private ShipyardConsoleMenu? _menu;

        [ViewVariables]
        public int Balance { get; private set; }

        public ShipyardConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            List<string> accessLevels;
            if (_entityManager.TryGetComponent<ShipyardConsoleComponent>(Owner.Owner, out var component))
            {
                accessLevels = component.AccessLevels;
                accessLevels.Sort();
            }
            else
            {
                accessLevels = new List<string>();
            }
            //TODO check for access either here or do it serverside and do a popup
            
            //Also we are bringing the sprite manager along for the future so we can flair up the menu with some icons later too, im just bad at UI design
            var sysManager = _entityManager.EntitySysManager;
            var spriteSystem = sysManager.GetEntitySystem<SpriteSystem>();
            _menu = new ShipyardConsoleMenu(this, IoCManager.Resolve<IPrototypeManager>(), spriteSystem, accessLevels);
            var description = new FormattedMessage();
            _menu.OpenCentered();
            _menu.OnClose += Close;
            _menu.OnOrderApproved += ApproveOrder;
            //_menu.OnSellShip += SellShip;
        }

        private void Populate()
        {
            if (_menu == null) return;

            _menu.PopulateProducts();
            _menu.PopulateCategories();
        }
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not ShipyardConsoleInterfaceState cState)
                return;

            Balance = cState.Balance;

            var castState = (ShipyardConsoleInterfaceState) state;
            Populate();
            _menu?.UpdateState(castState);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            _menu?.Dispose();
        }

        private void ApproveOrder(ButtonEventArgs args)
        {
            if (args.Button.Parent?.Parent is not VesselRow row || row.Vessel == null)
            {
                return;
            }

            var vesselId = row.Vessel.ID;
            var price = row.Vessel.Price;
            SendMessage(new ShipyardConsolePurchaseMessage(vesselId, price));
        }
        //private void SellShip(ButtonEventArgs args)
        //{
        //    //reserved for a sanity check, but im not sure what since we checked all the important stuffs on client already
        //    SendMessage(new ShipyardConsoleSellMessage());
        //}
    }
}
