using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Weapon.Ranged
{
    [RegisterComponent]
    public sealed class ServerRangedWeaponComponent : SharedRangedWeaponComponent
    {
        public TimeSpan LastFireTime;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clumsyCheck")]
        public bool ClumsyCheck { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clumsyExplodeChance")]
        public float ClumsyExplodeChance { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canHotspot")]
        public bool CanHotspot = true;

        [DataField("clumsyWeaponHandlingSound")]
        public SoundSpecifier ClumsyWeaponHandlingSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

        [DataField("clumsyWeaponShotSound")]
        public SoundSpecifier ClumsyWeaponShotSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg");

    }
}
