namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class MetapsionicPowerComponent : Component
    {
        [DataField("range")]
        public float Range = 10f;
    }
}