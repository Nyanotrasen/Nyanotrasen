using Content.Shared.Sound;

namespace Content.Server.Abilities.Gachi.Components
{
    [RegisterComponent]
    public sealed class GachiComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("painSound")]
        public SoundSpecifier PainSound { get; set; } = new SoundCollectionSpecifier("GachiPain");

        [DataField("hitOtherSound")]
        public SoundSpecifier HitOtherSound { get; set; } = new SoundCollectionSpecifier("GachiHitOther");
    }
}
