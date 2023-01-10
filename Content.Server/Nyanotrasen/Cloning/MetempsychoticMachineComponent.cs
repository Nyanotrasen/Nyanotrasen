namespace Content.Server.Cloning
{
    [RegisterComponent]
    public sealed class MetempsychoticMachineComponent : Component
    {
        /// <summary>
        /// Chance you will spawn as a humanoid instead of a non humanoid.
        /// </summary>
        [DataField("humanoidBaseChance")]
        public float HumanoidBaseChance = 0.75f;
    }
}
