namespace Content.Shared.Psionics.Glimmer
{
    [RegisterComponent]
    public class SharedGlimmerReactiveComponent : Component
    {
        /// <summary>
        /// Does this component try to modulate the strength of a PointLight
        /// component on the same entity based on the Glimmer tier?
        /// </summary>
        [DataField("modulatesPointLight")]
        public bool ModulatesPointLight = false;

        /// <summary>
        /// What is the correlation between the Glimmer tier and how strongly
        /// the light grows? The result is added to the base Energy.
        /// </summary>
        [DataField("glimmerToLightEnergyFactor")]
        public float GlimmerToLightEnergyFactor = 1.0f;

        /// <summary>
        /// What is the correlation between the Glimmer tier and how much
        /// distance the light covers? The result is added to the base Radius.
        /// </summary>
        [DataField("glimmerToLightRadiusFactor")]
        public float GlimmerToLightRadiusFactor = 1.0f;
    }
}
