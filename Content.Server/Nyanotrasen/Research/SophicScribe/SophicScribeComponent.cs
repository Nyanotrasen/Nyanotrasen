namespace Content.Server.Research.SophicScribe
{
    [RegisterComponent]
    public sealed class SophicScribeComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("announceInterval")]
        public TimeSpan AnnounceInterval = TimeSpan.FromMinutes(2);
    }
}
