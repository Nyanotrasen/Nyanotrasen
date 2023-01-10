using Content.Shared.Whitelist;
using Robust.Shared.Timing;

namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed class PsionicInvisibleContactsComponent : Component
    {
        [DataField("whitelist", required: true)]
        public EntityWhitelist Whitelist = default!;

        /// <summary>
        /// Last tick we did a failed exit check from.
        /// So, if you exit multiple webs on the same tick,
        /// you still lose invis.
        /// </summary>
        public GameTick LastFailedTick = default;
    }
}
