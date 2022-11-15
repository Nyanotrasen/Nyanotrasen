using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Borgs.UI
{
    [UsedImplicitly]
    public sealed class LawsBoundUserInterface : BoundUserInterface
    {
        private LawsMenu? _lawsMenu;

        public EntityUid Machine;
        public LawsBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
        {
            Machine = owner.Owner;
        }

        protected override void Open()
        {
            base.Open();

            _lawsMenu = new LawsMenu(this);

            _lawsMenu.OnClose += Close;

            _lawsMenu.OpenCentered();
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _lawsMenu?.Dispose();
        }
    }
}
