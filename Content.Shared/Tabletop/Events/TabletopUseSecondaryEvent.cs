using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    [Serializable, NetSerializable]
    public sealed class TabletopUseSecondaryEvent : EntityEventArgs
    {
        /// <summary>
        /// The UID of the entity being used.
        /// </summary>
        public EntityUid UsedEntityUid;

        public TabletopUseSecondaryEvent(EntityUid usedEntityUid)
        {
            UsedEntityUid = usedEntityUid;
        }
    }
}
