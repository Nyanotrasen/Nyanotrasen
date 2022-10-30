using System.Threading;

namespace Content.Shared.Arachne
{
    [RegisterComponent]
    public sealed class ArachneComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        [DataField("webDelay")]
        public float WebDelay = 5f;
    }
}
