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
    }
}
