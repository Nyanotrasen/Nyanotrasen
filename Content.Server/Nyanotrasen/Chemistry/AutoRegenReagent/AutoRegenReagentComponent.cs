using Content.Shared.Chemistry.Components;

namespace Content.Server.Chemistry.AutoRegenReagent
{
    [RegisterComponent]
    public sealed class AutoRegenReagentComponent : Component
    {
        [DataField("solution", required: true)]
        public string? SolutionName = null; // we'll fail during tests otherwise

        [DataField("reagent", required: true)]
        public string Reagent = default!;

        public Solution? Solution = default!;

        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("unitsPerSecond")]
        public float unitsPerSecond = 0.2f;
    }
}
