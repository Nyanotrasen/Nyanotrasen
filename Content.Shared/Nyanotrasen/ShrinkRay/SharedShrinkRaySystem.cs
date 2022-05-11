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

        /// <summary>
        /// True if we're resetting back to original size.
        /// Helps the client cleanup stuff.
        /// </summary>
        public bool Reset = false;
        public SizeChangedEvent(EntityUid target, Vector2 scale, bool reset = false)
        {
            Target = target;
            Scale = scale;
            Reset = reset;
        }
    }
}