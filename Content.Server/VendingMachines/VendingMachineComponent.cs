using Content.Server.UserInterface;
using Content.Shared.Sound;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.VendingMachines
{
    [RegisterComponent]
    public sealed class VendingMachineComponent : SharedVendingMachineComponent
    {
        public bool Ejecting;

        public TimeSpan AnimationDuration = TimeSpan.Zero;

        [ViewVariables] [DataField("pack", customTypeSerializer:typeof(PrototypeIdSerializer<VendingMachineInventoryPrototype>))]
        public string PackPrototypeId = string.Empty;

        public string SpriteName = "";

        public bool Broken;

        /// <summary>
        /// When true, will forcefully throw any object it dispenses
        /// </summary>
        [DataField("speedLimiter")]
        public bool CanShoot = false;

        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");

        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(VendingMachineUiKey.Key);

        public float NonLimitedEjectForce = 7.5f;

        public float NonLimitedEjectRange = 5f;
    }
}
