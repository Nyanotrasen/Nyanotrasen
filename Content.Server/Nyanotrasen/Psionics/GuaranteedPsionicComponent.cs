namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed class GuaranteedPsionicComponent : Component
    {
        [DataField("power", required: true)]
        public string PowerComponent = "";
    }
}
