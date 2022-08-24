namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed class PotentialPsionicComponent : Component
    {
        [DataField("chance")]
        public float Chance = 0.04f;
    }
}
