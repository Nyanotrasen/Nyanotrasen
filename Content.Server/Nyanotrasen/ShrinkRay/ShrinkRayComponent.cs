namespace Content.Server.ShrinkRay
{
    [RegisterComponent]
    public sealed class ShrinkRayComponent : Component
    {
        [DataField("applyItem")]
        public bool ApplyItem = true;

        [DataField("scaleFactor")]
        public Vector2 ScaleFactor = (0.3f, 0.3f);
    }
}
