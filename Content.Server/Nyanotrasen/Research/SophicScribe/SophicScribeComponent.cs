namespace Content.Server.Research.SophicScribe
{
    [RegisterComponent]
    public sealed class SophicScribeComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("announceInterval")]
        public TimeSpan AnnounceInterval = TimeSpan.FromMinutes(2);

        /// <summary>
        ///     Antispam.
        /// </summary>
        public TimeSpan? StateTime = null;

        [DataField("stateCD")]
        public TimeSpan StateCD = TimeSpan.FromSeconds(5);
    }
}
