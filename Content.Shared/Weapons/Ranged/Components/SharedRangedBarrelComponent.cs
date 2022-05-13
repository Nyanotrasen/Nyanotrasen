﻿namespace Content.Shared.Weapons.Ranged.Components
{
    public abstract class SharedRangedBarrelComponent : Component
    {
        [ViewVariables]
        public abstract FireRateSelector FireRateSelector { get; }
        [ViewVariables]
        public abstract FireRateSelector AllRateSelectors { get; }
        [ViewVariables]
        public abstract float FireRate { get; }
        [ViewVariables]
        public abstract int ShotsLeft { get; }
        [ViewVariables]
        public abstract int Capacity { get; }
    }

    [Flags]
    public enum FireRateSelector
    {
        Safety = 0,
        Single = 1 << 0,
        Automatic = 1 << 1,
    }
}
