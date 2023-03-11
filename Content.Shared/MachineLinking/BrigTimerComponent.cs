using Robust.Shared.Serialization;

namespace Content.Shared.MachineLinking;

[Serializable, NetSerializable, RegisterComponent]
public abstract class BrigTimerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Length { get; set; } = 60f;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Door { get; set; }
}
