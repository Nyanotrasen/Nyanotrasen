using Robust.Shared.Audio;
using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class PsionicRegenerationPowerComponent : Component
    {
        [DataField("soundUse")]
        // Credit to: https://freesound.org/people/greyseraphim/sounds/21409/
        public SoundSpecifier SoundUse = new SoundPathSpecifier("/Audio/Nyanotrasen/heartbeat_fast.ogg");

        public InstantAction? PsionicRegenerationPowerAction = null;
    }
}

