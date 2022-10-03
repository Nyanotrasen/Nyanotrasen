namespace Content.Server.Psionics.Glimmer
{
    [RegisterComponent]
    /// <summary>
    /// Adds to glimmer at regular intervals. We'll use it for glimmer drains too when we get there.
    /// </summary>
    public sealed class GlimmerSourceComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;
        /// <summary>
        /// Since glimmer is an int, we'll do it like this.
        /// </summary>
        [DataField("secondsPerGlimmer")]
        public float SecondsPerGlimmer = 15f;
    }
}
