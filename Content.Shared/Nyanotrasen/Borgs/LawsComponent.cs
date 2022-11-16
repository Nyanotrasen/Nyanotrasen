namespace Content.Shared.Borgs
{
    [RegisterComponent]
    public sealed class LawsComponent : Component
    {
        [DataField("laws")]
        public HashSet<string> Laws = default!;

        [DataField("canState")]
        public bool CanState = true;

        /// <summary>
        ///     Antispam.
        /// </summary>
        public TimeSpan? StateTime = null;

        [DataField("stateCD")]
        public TimeSpan StateCD = TimeSpan.FromSeconds(30);
    }
}
