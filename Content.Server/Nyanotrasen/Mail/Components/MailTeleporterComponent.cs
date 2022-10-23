using Robust.Shared.Audio;

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

        /// <summary>
        /// The sound that's played when new mail arrives.
        /// </summary>
        [DataField("teleportSound")]
        public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

        /// <summary>
        /// How many mail candidates do we need per actual delivery sent when
        /// the mail goes out? The number of candidates is divided by this number
        /// to determine how many deliveries will be teleported in.
        /// It does not determine unique recipients. That is random.
        /// </summary>
        [DataField("candidatesPerDelivery")]
        public int CandidatesPerDelivery = 8;

        [DataField("minimumDeliveriesPerTeleport")]
        public int MinimumDeliveriesPerTeleport = 1;

        /// <summary>
        /// Any item that breaks or is destroyed in less than this amount of
        /// damage is one of the types of items considered fragile.
        /// </summary>
        [DataField("fragileDamageThreshold")]
        public int FragileDamageThreshold = 10;

        /// <summary>
        /// What's the bonus for delivering a fragile package intact?
        /// </summary>
        [DataField("fragileBonus")]
        public int FragileBonus = 100;

        /// <summary>
        /// What's the malus for breaking a fragile package?
        /// </summary>
        [DataField("fragileMalus")]
        public int FragileMalus = -100;
    }
}
