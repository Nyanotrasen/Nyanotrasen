namespace Content.Server.Soul
{
    [RegisterComponent]
    public sealed class GolemComponent : Component
    {
        // we use these to config stuff via UI before installation
        public string? Master;
        public string? GolemName;

        public EntityUid? PotentialCrystal;
    }
}
