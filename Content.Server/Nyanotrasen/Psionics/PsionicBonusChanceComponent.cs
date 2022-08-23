namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed class PsionicBonusChanceComponent : Component
    {
        [DataField("multiplier")]
        public float Multiplier = 1f;
        [DataField("flatBonus")]
        public float FlatBonus = 0;
    }
}
