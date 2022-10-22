using Robust.Shared.Audio;
using Content.Shared.Storage;
using Content.Shared.Mail;

namespace Content.Server.Mail.Components
{
    [RegisterComponent]
    public sealed class MailComponent : SharedMailComponent
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("recipient")]
        public string Recipient = "None";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("recipientJob")]
        public string RecipientJob = "None";

        // Why do we not use LockComponent?
        // Because this can't be locked again,
        // and we have special conditions for unlocking,
        // and we don't want to add a verb.
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("locked")]
        public bool Locked = true;

        /// <summary>
        /// What will be packaged when the mail is spawned.
        /// </summary>
        [DataField("contents")]
        public List<EntitySpawnEntry> Contents = new();

        /// <summary>
        /// The amount that cargo will be awarded for delivering this mail.
        /// </summary>
        [DataField("bounty")]
        public int Bounty = 750;

        /// <summary>
        /// Penalty if the mail is destroyed.
        /// </summary>
        [DataField("penalty")]
        public int Penalty = -250;

        /// <summary>
        /// The sound that's played when the mail's lock is broken.
        /// </summary>
        [DataField("penaltySound")]
        public SoundSpecifier PenaltySound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

        /// <summary>
        /// The sound that's played when the mail's opened.
        /// </summary>
        [DataField("openSound")]
        public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/Effects/packetrip.ogg");

        /// <summary>
        /// Whether this component is enabled.
        /// Removed when it becomes trash.
        /// </summary>
        public bool Enabled = true;
    }
}
