namespace Content.Shared.ShrinkRay
{
    public sealed class SharedShrinkRaySystem : EntitySystem
    {
        public void RemoveShrunken(EntityUid uid)
        {
            RemComp<ShrunkenComponent>(uid);
            RemComp<ShrunkenSpriteComponent>(uid);
        }
    }
}