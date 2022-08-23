namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class GuaranteedPsionicComponent : Component
    {
        [DataField("power")]
        public string? PowerComponent;
    }
}
