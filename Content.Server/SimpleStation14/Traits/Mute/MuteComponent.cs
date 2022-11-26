namespace Content.Server.SimpleStation14.Traits.Mute
{
    /// <summary>
    /// Owner entity cannot speak.
    /// </summary>
    [RegisterComponent]
    public sealed class MuteTraitComponent : Component
    {
        /// <summary>
        /// Whether this component is active or not.
        /// </summarY>
        [ViewVariables]
        [DataField("enabled")]
        public bool Enabled = true;
    }
}
