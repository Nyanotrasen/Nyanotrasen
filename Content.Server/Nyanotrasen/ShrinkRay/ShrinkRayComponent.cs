namespace Content.Server.ShrinkRay
{
    [RegisterComponent]
    public sealed class ShrinkRayComponent : Component
    {
        public bool ApplyItem = false;

        [DataField("scaleFactor")]
        public Vector2 ScaleFactor = (0.3f, 0.3f);
    }
}
