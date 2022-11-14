using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.Chapel
{
    [RegisterComponent]
    public sealed class SacrificialAltarComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        [DataField("sacrificeTime")]
        public TimeSpan SacrificeTime = TimeSpan.FromSeconds(8.35);

        [DataField("sacrificeSound")]
        public SoundSpecifier SacrificeSoundPath = new SoundPathSpecifier("/Audio/Effects/clang2.ogg");

        [DataField("finishSound")]
        public SoundSpecifier FinishSound = new SoundPathSpecifier("/Audio/Effects/gib1.ogg");

        public IPlayingAudioStream? SacrificeStingStream;

        [DataField("requiresBibleUser")]
        public bool RequiresBibleUser = true;

        [DataField("rewardPool")]
        public string RewardPool = "PsionicArtifactPool";

        [DataField("bluespaceRewardMin")]
        public int BluespaceRewardMin = 1;

        [DataField("bluespaceRewardMax")]
        public int BlueSpaceRewardMax = 4;

        [DataField("glimmerReductionMin")]
        public int GlimmerReductionMin = 50;

        [DataField("glimmerReductionMax")]
        public int GlimmerReductionMax = 100;

        [DataField("trapPrototype")]
        public string TrapPrototype = "CrystalSoul";
    }
}
