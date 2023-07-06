using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;


namespace Content.Client.CartridgeLoader.Cartridges;

public sealed class GlimmerMonitorUi : UIFragment
{
    private GlimmerMonitorUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new GlimmerMonitorUiFragment();

        _fragment.OnSync += _ => SendSyncMessage(userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not GlimmerMonitorUiState monitorState)
            return;

        _fragment?.UpdateState(monitorState.GlimmerValues);
    }

    private void SendSyncMessage(BoundUserInterface userInterface)
    {
        var syncMessage = new GlimmerMonitorSyncMessageEvent();
        var message = new CartridgeUiMessage(syncMessage);
        userInterface.SendMessage(message);
    }
}
