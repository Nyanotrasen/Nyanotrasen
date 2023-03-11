using Content.Server.Chat.Systems;

namespace Content.Server.Radio;

public sealed class RadioReceiveEvent : EntityEventArgs
{
    public readonly EntityChat Chat;
    public readonly EntityChatData RecipientData;

    public RadioReceiveEvent(EntityChat chat, EntityChatData recipientData)
    {
        Chat = chat;
        RecipientData = recipientData;
    }
}

public sealed class RadioReceiveAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityChat Chat;

    public RadioReceiveAttemptEvent(EntityChat chat)
    {
        Chat = chat;
    }
}
