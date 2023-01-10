namespace Content.Server.Cloning
{
    /// <summary>
    /// This tracks how many times you have already been cloned and lowers your chance of getting a humanoid each time.
    /// </summary>
    [RegisterComponent]
    public sealed class MetempsychosisKarmaComponent : Component
    {
        [DataField("score")]
        public int Score = 0;
    }
}
