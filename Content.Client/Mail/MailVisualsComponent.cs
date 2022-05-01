namespace Content.Client.Mail;

/// <summary>
/// Holds the locked state for mail.
/// </summary>
[RegisterComponent]
public sealed class MailVisualsComponent : Component
{
    [DataField("isLocked")]
    public bool IsLocked = true;
}
