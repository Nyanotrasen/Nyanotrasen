using Content.Shared.ReverseEngineering;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Nyanotrasen.ReverseEngineering;

[UsedImplicitly]
public sealed class ReverseEngineeringMachineBoundUserInterface : BoundUserInterface
{
    private ReverseEngineeringMachineMenu? _revMenu;

    public ReverseEngineeringMachineBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _revMenu = new ReverseEngineeringMachineMenu();

        _revMenu.OnClose += Close;
        _revMenu.OpenCentered();

        _revMenu.OnScanButtonPressed += _ =>
        {
            SendMessage(new ReverseEngineeringMachineScanButtonPressedMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case ReverseEngineeringMachineScanUpdateState msg:
                _revMenu?.SetButtonsDisabled(msg);
                _revMenu?.UpdateInformationDisplay(msg);
                _revMenu?.UpdateProbeTickProgressBar(msg);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;
        _revMenu?.Dispose();
    }
}

