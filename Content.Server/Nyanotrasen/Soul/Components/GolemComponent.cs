using Robust.Shared.Audio;

namespace Content.Server.Soul
{
    [RegisterComponent]
    public sealed class GolemComponent : Component
    {
        // we use these to config stuff via UI before installation
        public string? Master;
        public string? GolemName;
        public EntityUid? PotentialCrystal;

        [DataField("deathSound")]
        public SoundSpecifier DeathSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/Lightning/lightningbolt.ogg");
    }
}
