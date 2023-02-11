namespace Content.Server.Weapons.Melee
{
    /// <summary>
    /// This component allows melee weapons to tire their user with each swing.
    /// </summary>
    [RegisterComponent]
    public sealed class MeleeStaminaCostComponent : Component
    {
        /// <summary>
        /// How much stamina does it cost to swing this weapon?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("swing")]
        public float SwingCost { get; set; }

        /// <summary>
        /// How much additional stamina does it cost on a successful hit?
        /// </summary>
        /// <remarks>
        /// This value is compounded with the swing cost.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("hit")]
        public float HitCost { get; set; }

        /// <summary>
        /// Added to cost if weapon is wielded. Applied before heavy cost which is a bit of a buff to wielding.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("wieldModifier")]
        public float WieldModifier { get; set; } = 5f;

        /// <summary>
        /// Stamina cost modifier for heavy attack.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("heavyStaminaCostModifier")]
        public float HeavyStaminaCostModifier = 2f;
    }
}
