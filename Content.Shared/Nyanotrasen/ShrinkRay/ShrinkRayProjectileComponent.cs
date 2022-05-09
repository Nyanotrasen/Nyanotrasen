using Robust.Shared.GameStates;

namespace Content.Shared.ShrinkRay
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class ShrinkRayProjectileComponent : Component
    {
        [DataField("scaleFactor")]
        public Vector2 ScaleFactor = (0.3f, 0.3f);

        [ViewVariables]
        [DataField("applyItem")]
        public bool ApplyItem = true;
    }
}
