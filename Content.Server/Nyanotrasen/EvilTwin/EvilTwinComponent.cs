using Content.Server.Mind.Components;

namespace Content.Server.EvilTwin
{
    [RegisterComponent]
    public sealed class EvilTwinComponent : Component
    {
        public Mind.Mind? TwinMind;
    }
}
