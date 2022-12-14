using Robust.Shared.Prototypes;

namespace Content.Server.Research.Oracle
{
    [RegisterComponent]
    public sealed class OracleComponent : Component
    {
        public const string SolutionName = "fountain";

        [ViewVariables]
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [ViewVariables]
        [DataField("resetTime")]
        public TimeSpan ResetTime = TimeSpan.FromMinutes(10);

        [DataField("barkAccumulator")]
        public float BarkAccumulator = 0f;

        [DataField("barkTime")]
        public TimeSpan BarkTime = TimeSpan.FromMinutes(1);

        [ViewVariables(VVAccess.ReadWrite)]
        public EntityPrototype DesiredPrototype = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public EntityPrototype? LastDesiredPrototype = default!;
    }
}
