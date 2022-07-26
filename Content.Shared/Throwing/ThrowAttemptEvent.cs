namespace Content.Shared.Throwing
{
    public sealed class ThrowAttemptEvent : CancellableEntityEventArgs
    {
        public ThrowAttemptEvent(EntityUid uid, EntityUid itemUid)
        {
            Uid = uid;
            itemUid = uid;
        }

        public EntityUid Uid { get; }

        public EntityUid itemUid { get; }
    }

    /// <summary>
    /// Raised when we try to pushback an entity from throwing
    /// </summary>
    public sealed class ThrowPushbackAttemptEvent : CancellableEntityEventArgs {}
}
