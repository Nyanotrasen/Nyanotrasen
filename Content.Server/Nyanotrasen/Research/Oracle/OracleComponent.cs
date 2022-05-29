using Robust.Shared.Prototypes;

namespace Content.Server.Research.Oracle
{
    [RegisterComponent]
    public sealed class OracleComponent : Component
    {
        [ViewVariables]
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [ViewVariables]
        [DataField("resetTime")]
        public TimeSpan ResetTime = TimeSpan.FromMinutes(1);

        [DataField("barkAccumulator")]
        public float BarkAccumulator = 0f;

        [DataField("barkTime")]
        public TimeSpan BarkTime = TimeSpan.FromSeconds(30);

        [ViewVariables(VVAccess.ReadWrite)]
        public EntityPrototype DesiredPrototype = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public EntityPrototype? LastDesiredPrototype = default!;
    }
}
