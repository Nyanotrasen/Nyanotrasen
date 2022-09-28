using System.Threading;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class HeadCageComponent : Component
    {
        public CancellationTokenSource? CancelToken;
        public bool IsActive = false;
    }
}