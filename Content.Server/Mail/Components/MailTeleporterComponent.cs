namespace Content.Server.Mail.Components
{
    /// <summary>
    /// This is for the mail teleporter.
    /// Random mail will be teleported to this every few minutes.
    /// </summary>
    [RegisterComponent]
    public sealed class MailTeleporterComponent : Component
    {

        // Not starting accumulator at 0 so mail carriers have some deliveries to make shortly after roundstart.
        [DataField("accumulator")]
        public float Accumulator = 285f;

        [DataField("teleportInterval")]
        public TimeSpan teleportInterval = TimeSpan.FromMinutes(5);
    }
}
