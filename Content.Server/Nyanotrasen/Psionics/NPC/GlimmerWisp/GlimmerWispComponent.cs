using System.Threading;

namespace Content.Server.Psionics.NPC.GlimmerWisp
{
    [RegisterComponent]
    public sealed class GlimmerWispComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// The time (in seconds) that it takes to drain an entity.
        /// </summary>
        [DataField("drainDelay")]
        public float DrainDelay = 5.0f;
    }
}
