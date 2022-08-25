using Content.Shared.Damage;

namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed class AntiPsionicWeaponComponent : Component
    {

        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;

        [DataField("psychicDamageMultiplier")]
        public float PsychicDamageMultiplier = 1.5f;

        [DataField("disableChance")]
        public float DisableChance = 0.3f;
    }
}
