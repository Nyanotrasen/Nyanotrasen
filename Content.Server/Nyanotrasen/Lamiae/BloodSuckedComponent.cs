namespace Content.Server.Lamiae
{
    /// <summary>
    /// For entities who have been succed.
    /// </summary>
    [RegisterComponent]
    public sealed class BloodSuckedComponent : Component
    {
        /// <summary>
        /// How much to succ each time we succ.
        /// </summary>
        public float UnitsToSucc = 20f;
    }
}
