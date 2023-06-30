using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class GlimmerMonitorUiState : BoundUserInterfaceState
{
    public List<int> GlimmerValues;

    public GlimmerMonitorUiState(List<int> glimmerValues)
    {
        GlimmerValues = glimmerValues;
    }
}
