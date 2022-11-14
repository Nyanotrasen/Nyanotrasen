using System.Threading;

namespace Content.Server.Chapel
{
    [RegisterComponent]
    public sealed class SacrificialAltarComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        [DataField("sacrificeTime")]
        public TimeSpan SacrificeTime = TimeSpan.FromSeconds(8);

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
