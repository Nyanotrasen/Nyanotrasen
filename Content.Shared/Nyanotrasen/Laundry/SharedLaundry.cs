using Robust.Shared.Serialization;

namespace Content.Shared.Laundry;

[RegisterComponent]
public sealed class SharedWashingMachineComponent : Component { }

[Serializable, NetSerializable]
public enum WashingMachineVisualState : byte
{
    Broken,
}
