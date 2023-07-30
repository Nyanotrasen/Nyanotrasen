using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Melee.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedMeleeWeaponSystem))]
public sealed class BonusMeleeAttackRateComponent : Component
{
    /// <summary>
    /// The value added onto the attack rate of a melee weapon
    /// </summary>
    [DataField("flatModifier"), ViewVariables(VVAccess.ReadWrite)]
    public float FlatModifier;

    /// <summary>
    /// A value that is multiplied by the attack rate of a melee weapon
    /// </summary>
    [DataField("multiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float Multiplier = 1;
}
