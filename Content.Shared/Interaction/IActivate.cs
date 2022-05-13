using JetBrains.Annotations;

namespace Content.Shared.Interaction
{
    /// <summary>
    ///     This interface gives components behavior when being activated (by default,
    ///     this is done via the "E" key) when the user is in range and has unobstructed access to the target entity
    ///     (allows inside blockers). This includes activating an object in the world as well as activating an
    ///     object in inventory. Unlike IUse, this can be performed on entities that aren't in the active hand,
    ///     even when the active hand is currently holding something else.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IActivate
    {
        /// <summary>
        ///     Called when this component is activated by another entity who is in range.
        /// </summary>
        [Obsolete("Use ActivateInWorldMessage instead")]
        void Activate(ActivateEventArgs eventArgs);
    }

    public sealed class ActivateEventArgs : EventArgs, ITargetedInteractEventArgs
    {
        public ActivateEventArgs(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }

        public EntityUid User { get; }
        public EntityUid Target { get; }
    }

    /// <summary>
    ///     Raised when an entity is activated in the world.
    /// </summary>
    [PublicAPI]
    public sealed class ActivateInWorldEvent : HandledEntityEventArgs, ITargetedInteractEventArgs
    {
        /// <summary>
        ///     Entity that activated the target world entity.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Entity that was activated in the world.
        /// </summary>
        public EntityUid Target { get; }

        public ActivateInWorldEvent(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }
    }
}
