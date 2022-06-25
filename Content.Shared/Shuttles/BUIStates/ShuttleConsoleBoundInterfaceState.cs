using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public readonly ShuttleMode Mode;

    public ShuttleConsoleBoundInterfaceState(
        ShuttleMode mode,
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks) : base(maxRange, coordinates, angle, docks)
    {
        Mode = mode;
    }
}
