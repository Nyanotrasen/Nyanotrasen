using Content.Server.Chat.Systems;

namespace Content.Server.Radio;

[ByRefEvent]
public struct RadioReceiveEvent
{
    public readonly EntityChat Chat;
    public readonly EntityChatData RecipientData;

    public RadioReceiveEvent(EntityChat chat, EntityChatData recipientData)
    {
        Chat = chat;
        RecipientData = recipientData;
    }
}

/// <summary>
/// Use this event to cancel sending messages by doing various checks (e.g. range)
/// </summary>
[ByRefEvent]
public struct RadioReceiveAttemptEvent
{
    public readonly EntityChat Chat;
    public readonly EntityUid RadioReceiver;

    public bool Cancelled;

    public RadioReceiveAttemptEvent(EntityChat chat, EntityUid radioReceiver)
    {
        Chat = chat;
        RadioReceiver = radioReceiver;
    }
}
