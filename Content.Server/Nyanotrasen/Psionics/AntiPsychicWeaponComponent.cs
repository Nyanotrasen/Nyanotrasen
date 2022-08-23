using Content.Shared.Damage;

namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed class AntiPsionicWeaponComponent : Component
    {

        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;

        [DataField("psychicDamageMultiplier")]
        public float PsychicDamageMultiplier = 2f;

        [DataField("stunChance")]
        public float StunChance = 0.2f;
    }
}
