using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class GlimmerMonitorUiState : BoundUserInterfaceState
{
    public int Glimmer;

    public GlimmerMonitorUiState(int glimmer)
    {
        Glimmer = glimmer;
    }
}
