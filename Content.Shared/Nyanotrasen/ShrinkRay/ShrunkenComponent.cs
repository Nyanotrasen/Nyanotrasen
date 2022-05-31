using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.ShrinkRay
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class ShrunkenComponent : Component
    {
        public Vector2 ScaleFactor = (0.3f, 0.3f);
        public Vector2 OriginalScaleFactor = (1f, 1f);

        public Dictionary<Fixture, float> OriginalRadii = new();

        [ViewVariables]
        public double MassScale = 0.027;

        [ViewVariables]
        public bool ShouldHaveItemComp = true;

        [ViewVariables]
        public float Accumulator = 0f;
        [ViewVariables]
        public TimeSpan ShrinkTime = TimeSpan.FromSeconds(30);
    }
}
