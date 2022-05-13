namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    [RegisterComponent]
    public sealed class ChemicalAmmoComponent : Component
    {
        public const string DefaultSolutionName = "ammo";

        [DataField("solution")]
        public string SolutionName { get; set; } = DefaultSolutionName;
    }
}
