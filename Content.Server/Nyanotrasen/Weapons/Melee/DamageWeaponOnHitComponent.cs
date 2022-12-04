using Content.Shared.Damage;

namespace Content.Server.Weapons.Melee
{
    /// <summary>
    /// This component allows melee weapons to degrade with each successful hit,
    /// similar to how DamageOnLand interacts with throwing.
    /// </summary>
    /// <remarks>
    /// The entity in question will still need Damageable and Destructible
    /// components to factor in any damage it would take.
    /// </remarks>
    [RegisterComponent]
    public sealed class DamageWeaponOnHitComponent : Component
    {
        /// <summary>
        /// How much damage will the weapon take on each hit?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("damage", required: true)]
        public DamageSpecifier Damage { get; set; } = default!;
    }
}
