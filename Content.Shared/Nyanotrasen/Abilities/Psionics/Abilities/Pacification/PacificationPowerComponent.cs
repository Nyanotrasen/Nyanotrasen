namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class PacificationPowerComponent : Component
    {
        [DataField("pacifyTime")]
        public TimeSpan PacifyTime = TimeSpan.FromSeconds(20);
    }
}