using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed class FinitePowerSupplierComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("decayRate")]
    public float DecayRate = 0.995f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("decayInterval")]
    public TimeSpan DecayInterval = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("nextDecay", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextDecayTick = TimeSpan.Zero;
}
