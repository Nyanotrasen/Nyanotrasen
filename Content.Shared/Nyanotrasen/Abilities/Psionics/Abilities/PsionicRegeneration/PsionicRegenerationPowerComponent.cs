using Robust.Shared.Audio;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.DoAfter;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class PsionicRegenerationPowerComponent : Component
    {
        [DataField("essence")]
        public float EssenceAmount = 20;

        [DataField("useDelay")]
        public float UseDelay = 8f;

        [DataField("soundUse")]
        public SoundSpecifier SoundUse = new SoundPathSpecifier("/Audio/Nyanotrasen/heartbeat_fast.ogg");
        /// <summary>
        /// When we started the last doafter.
        /// </summary>
        public DateTime StartedAt;

        public Shared.DoAfter.DoAfter? DoAfter;
        public InstantAction? PsionicRegenerationPowerAction = null;
    }
}

