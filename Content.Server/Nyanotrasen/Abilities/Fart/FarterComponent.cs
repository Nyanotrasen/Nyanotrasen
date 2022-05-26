using Content.Shared.Sound;
using Robust.Shared.Audio;

namespace Content.Server.Abilities.Fart
{
    [RegisterComponent]
    public sealed class FarterComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("fartSound")]
        public SoundSpecifier FartSound { get; set; } = new SoundCollectionSpecifier("Fart");

        public IPlayingAudioStream? Stream;
    }
}
