namespace Content.Server.Lamiae
{
    /// <summary>
    /// Lamia segment
    /// </summary>
    [RegisterComponent]
    public sealed class LamiaSegmentComponent : Component
    {
        public EntityUid AttachedToUid = default!;
        public int SegmentNumber = default!;
    }
}
