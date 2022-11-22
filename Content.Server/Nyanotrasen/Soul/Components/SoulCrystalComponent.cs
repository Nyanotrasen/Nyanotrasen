namespace Content.Server.Soul
{
    [RegisterComponent]
    public sealed class SoulCrystalComponent : Component
    {
        /// <summary>
        /// Basically, the identity of the soul inside this entity.
        /// </summary>
        [DataField("trueName")]
        public string? TrueName;
    }
}
