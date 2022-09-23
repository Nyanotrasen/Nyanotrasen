namespace Content.Server.Lamiae
{
    [RegisterComponent]
    /// <summary>
    /// Item that gives a bloodsucker injection glands (for poison, usually)
    /// </summary>
    public sealed class BloodSuckerGlandInjectorComponent : Component
    {
        public bool Used = false;

        /// <summary>
        /// How many units of our injected chem to inject.
        /// </summary>
        [DataField("unitsToInject")]
        public float UnitsToInject = 5;

        /// <summary>
        /// Which reagent to inject.
        /// </summary>
        [DataField("injectReagent")]
        public string InjectReagent = "";
    }
}
