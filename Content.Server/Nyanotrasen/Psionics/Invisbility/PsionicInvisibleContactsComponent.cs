using Content.Shared.Whitelist;

namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed class PsionicInvisibleContactsComponent : Component
    {
        [DataField("whitelist", required: true)]
        public EntityWhitelist Whitelist = default!;
    }
}
