using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.Silicons.Bots
{
    [RegisterComponent]
    public sealed class CleanBotComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// Used in NPC logic.
        /// </summary>
        [ViewVariables]
        public EntityUid? CleanTarget;

        /// <summary>
        /// How many units we can remove upon doafter completion.
        /// </summary>
        [DataField("UnitsToClean")]
        public float UnitsToClean = 10f;

        /// <summary>
        /// Doafter length, in seconds.
        /// </summary>
        [DataField("cleanDelay")]
        public float CleanDelay = 3.5f;

        [DataField("cleanSound")]
        public SoundSpecifier CleanSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");
    }
}
