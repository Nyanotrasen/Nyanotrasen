namespace Content.Client.Mail;

/// <summary>
/// Holds the locked state for mail.
/// </summary>
[RegisterComponent]
public sealed class MailVisualsComponent : Component
{
    [DataField("normalState", required: true)]
    public string NormalState = default!;

    [DataField("trashState", required: true)]
    public string TrashState = default!;

    [DataField("isLocked")]
    public bool IsLocked = true;
}
