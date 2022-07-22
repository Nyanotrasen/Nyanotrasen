using System.Threading;

namespace Content.Server.Lamiae
{
    [RegisterComponent]
    public sealed class BloodSuckerComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// How much to succ each time we succ.
        /// </summary>
        public float UnitsToSucc = 20f;

        /// <summary>
        /// The target that we could succ.
        /// </summary>
        public EntityUid? PotentialTarget = null;

        /// <summary>
        /// The time (in seconds) that it takes to succ an entity.
        /// </summary>
        [DataField("succDelay")]
        public float SuccDelay = 4.0f;
    }
}
