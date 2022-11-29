namespace Content.Server.Fugitive
{
    [RegisterComponent]
    public sealed class FugitiveCountdownComponent : Component
    {
        public TimeSpan? AnnounceTime = null;

        [DataField("AnnounceCD")]
        public TimeSpan AnnounceCD = TimeSpan.FromMinutes(5);
    }
}
