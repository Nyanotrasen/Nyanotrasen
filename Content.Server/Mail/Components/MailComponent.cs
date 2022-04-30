namespace Content.Server.Mail.Components
{
    [RegisterComponent]
    public sealed class MailComponent : Component
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
    }
}
