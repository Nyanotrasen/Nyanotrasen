using Robust.Shared.GameStates;

namespace Content.Shared.ShrinkRay
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class ShrunkenComponent : Component
    {
        public Vector2 ScaleFactor = (0.3f, 0.3f);
        public Vector2 OriginalScaleFactor = (1f, 1f);

        [ViewVariables]
        public double MassScale = 0.027;

        [ViewVariables]
        public bool WasOriginallyItem = true;
        [ViewVariables]
        public float Accumulator = 0f;
        [ViewVariables]
        public TimeSpan ShrinkTime = TimeSpan.FromSeconds(30);
    }
}
