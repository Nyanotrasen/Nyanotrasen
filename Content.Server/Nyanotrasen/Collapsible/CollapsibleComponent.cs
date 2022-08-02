using Robust.Shared.Audio;

namespace Content.Server.Collapsible
{
    [RegisterComponent]
    public sealed class CollapsibleComponent : Component
    {
        public bool Collapsed = true;
        public SoundSpecifier? ExtendSound = new SoundPathSpecifier("/Audio/Weapons/batonextend.ogg");
    }
}
