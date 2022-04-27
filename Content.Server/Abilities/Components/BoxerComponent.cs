namespace Content.Server.Abilities.Boxer
{
    /// <summary>
    /// Added to the boxer on spawn.
    /// </summary>
    [RegisterComponent]
    public sealed class BoxerComponent : Component
    {
        public bool Enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceNoSlowdown")]
        public float ParalyzeChanceNoSlowdown { get; set; } = 0.2f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceWithSlowdown")]
        public float ParalyzeChanceWithSlowdown { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; set; } = 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("slowdownTime")]
        public float SlowdownTime { get; set; } = 3f;
    }
}
