using Robust.Shared.Serialization;

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

    [Serializable, NetSerializable]
    public sealed class SizeChangedEvent : EntityEventArgs
    {
        public EntityUid Target;
        public Vector2 Scale;
        public SizeChangedEvent(EntityUid target, Vector2 scale)
        {
            Target = target;
            Scale = scale;
        }
    }
}