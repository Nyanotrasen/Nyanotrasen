using Content.Shared.ReverseEngineering;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Nyanotrasen.ReverseEngineering;

[UsedImplicitly]
public sealed class ReverseEngineeringMachineBoundUserInterface : BoundUserInterface
{
    private ReverseEngineeringMachineMenu? _consoleMenu;

    public ReverseEngineeringMachineBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _consoleMenu = new ReverseEngineeringMachineMenu();

        _consoleMenu.OnClose += Close;
        _consoleMenu.OpenCentered();

        _consoleMenu.OnServerSelectionButtonPressed += _ =>
        {
            SendMessage(new ReverseEngineeringMachineServerSelectionMessage());
        };
        _consoleMenu.OnScanButtonPressed += _ =>
        {
            SendMessage(new ReverseEngineeringMachineScanButtonPressedMessage());
        };
        _consoleMenu.OnPrintButtonPressed += _ =>
        {
            SendMessage(new ReverseEngineeringMachinePrintButtonPressedMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case ReverseEngineeringMachineScanUpdateState msg:
                _consoleMenu?.SetButtonsDisabled(msg);
                _consoleMenu?.UpdateInformationDisplay(msg);
                _consoleMenu?.UpdateProgressBar(msg);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;
        _consoleMenu?.Dispose();
    }
}

