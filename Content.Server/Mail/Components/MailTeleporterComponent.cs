namespace Content.Server.Mail.Components
{
    /// <summary>
    /// This is for the mail teleporter.
    /// Random mail will be teleported to this every few minutes.
    /// </summary>
    [RegisterComponent]
    public sealed class MailTeleporterComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("teleportInterval")]
        public TimeSpan teleportInterval = TimeSpan.FromSeconds(5);
    }
}
