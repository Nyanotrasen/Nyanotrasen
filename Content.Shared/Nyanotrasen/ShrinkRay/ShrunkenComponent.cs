using Robust.Shared.GameStates;

namespace Content.Shared.ShrinkRay
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class SharedShrunkenComponent : Component
    {
        public Vector2 ScaleFactor = (0.3f, 0.3f);

        public Vector2 OriginalScaleFactor = (1f, 1f);
    }
}
