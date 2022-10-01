namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class TinfoilHatComponent : Component
    {
        public bool IsActive = false;

        [DataField("passthrough")]
        public bool Passthrough = false;
    }
}