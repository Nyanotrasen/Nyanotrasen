using Robust.Shared.GameStates;

namespace Content.Shared.Lamiae
{
    /// <summary>
    /// Lamia segment
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class LamiaSegmentComponent : Component
    {
        public EntityUid AttachedToUid = default!;

        public EntityUid Lamia = default!;
        public int SegmentNumber = default!;
    }
}
