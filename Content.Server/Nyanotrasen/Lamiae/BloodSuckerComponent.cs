namespace Content.Server.Lamiae
{
    [RegisterComponent]
    public sealed class BloodSuckerComponent : Component
    {
        /// <summary>
        /// How much to succ each time we succ.
        /// </summary>
        public float UnitsToSucc = 20f;
    }
}
