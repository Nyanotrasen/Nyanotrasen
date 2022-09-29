using System.Threading;
using Robust.Shared.Audio;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class HeadCageComponent : Component
    {
        public CancellationTokenSource? CancelToken;
        public bool IsActive = false;

        [DataField("startBreakoutSound")]
        public SoundSpecifier StartBreakoutSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_breakout_start.ogg");

        [DataField("endCageSound")]
        public SoundSpecifier EndCageSound { get; set; } = new SoundPathSpecifier("/Audio/Items/Handcuffs/cuff_takeoff_end.ogg");
    }
}