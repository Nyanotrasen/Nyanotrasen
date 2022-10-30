﻿using Content.Shared.Body.Components;

namespace Content.Shared.Body.Part
{
    /// <summary>
    ///     This interface gives components behavior when a body part
    ///     is added to their owning entity.
    /// </summary>
    public interface IBodyPartAdded : IComponent
    {
        /// <summary>
        ///     Called when a <see cref="BodyComponent"/> is added to the
        ///     entity owning this component.
        /// </summary>
        /// <param name="args">Information about the part that was added.</param>
        void BodyPartAdded(BodyPartAddedEventArgs args);
    }

    public sealed class BodyPartAddedEventArgs : EventArgs
    {
        public BodyPartAddedEventArgs(string slot, BodyPartComponent part)
        {
            Slot = slot;
            Part = part;
        }

        /// <summary>
        ///     The slot that <see cref="Part"/> was added to.
        /// </summary>
        public string Slot { get; }

        /// <summary>
        ///     The part that was added.
        /// </summary>
        public BodyPartComponent Part { get; }
    }
}
