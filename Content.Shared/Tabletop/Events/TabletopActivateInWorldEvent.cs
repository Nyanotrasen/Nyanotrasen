using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    [Serializable, NetSerializable]
    public sealed class TabletopActivateInWorldEvent : EntityEventArgs
    {
        /// <summary>
        /// The UID of the entity being activated.
        /// </summary>
        public EntityUid ActivatedEntityUid;

        public TabletopActivateInWorldEvent(EntityUid activatedEntityUid)
        {
            ActivatedEntityUid = activatedEntityUid;
        }
    }
}
