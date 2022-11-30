using Content.Shared.Damage;

namespace Content.Server.Weapons.Melee
{
    /// <summary>
    /// This component allows melee weapons the ability to inflict bleeding
    /// status effects on their targets without doing any real damage,
    /// while also being able to be resisted by armor.
    /// </summary>
    [RegisterComponent]
    public sealed class MeleeBloodletterComponent : Component
    {
        /// <summary>
        /// How much will the target's bleeding rate be increased on each hit?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("bleedingIncrease")]
        public DamageSpecifier? BleedingIncrease { get; set; }

        /// <summary>
        /// How much blood is reduced from the target's bloodstream on each hit?
        /// </summary>
        /// <remarks>
        /// Blood reduction is distinct from the Bloodloss damage type.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("bloodReduction")]
        public DamageSpecifier? BloodReduction { get; set; }

        /// <summary>
        /// How much is the damage muliplied by when the weapon is Wielded?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("wieldCoefficient")]
        public float WieldCoefficient { get; set; } = 1.5f;
    }
}
