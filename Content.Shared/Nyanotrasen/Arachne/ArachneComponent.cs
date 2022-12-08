using System.Threading;

namespace Content.Shared.Arachne
{
    [RegisterComponent]
    public sealed class ArachneComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        [DataField("webDelay")]
        public float WebDelay = 5f;

        /// <summary>
        /// Blood reagent required to web up a mob.
        /// </summary>

        [DataField("webBloodReagent")]
        public string WebBloodReagent = "Blood";
    }
}
