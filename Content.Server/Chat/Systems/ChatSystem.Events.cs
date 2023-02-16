using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server.Chat.Systems
{
    public class EntityChat
    {
        public EntityUid Source;
        public string Message;
        public Dictionary<EntityUid, object> Recipients;
        public ChatChannel Channel;

        // Has a system claimed this chat message for their own?
        public Type? ClaimedBy;

        public string? SourceName;
        public object? Data;

        public EntityChat(EntityUid source, string message)
        {
            Source = source;
            Message = message;
            Recipients = new();
        }
    }

    public class EntityChatSpokenData
    {
        public string? Language;
        public RadioChannelPrototype? RadioChannel;
    }

    public class EntityChatSpokenRecipientData
    {
        public float Distance;
        public string? Message;

        public EntityChatSpokenRecipientData(float distance, string? message = null)
        {
            Distance = distance;
            Message = message;
        }
    }

    // New Chat flow:
    //
    // Parse                    - we have no idea what we have
    // Attempt                  - now we know what type of chat this is, do we continue?
    // GetRecipients            - we get who will receive this chat
    // Transform                - we can change what the speaker says
    // BeforeChat

    // GotEntityChatTransform   - we can change what the recipient hears
    // GotEntityChat            - finally, show the user the message

    [ByRefEvent]
    public struct EntityChatParseEvent
    {
        public EntityChat Chat;

        public EntityChatParseEvent(EntityChat chat)
        {
            Chat = chat;
        }

        public bool Handled;
    }

    [ByRefEvent]
    public struct EntityChatAttemptEvent
    {
        public EntityChat Chat;

        public EntityChatAttemptEvent(EntityChat chat)
        {
            Chat = chat;
        }

        public bool Cancelled { get; private set; }
        public void Cancel() => Cancelled = true;
        public void Uncancel() => Cancelled = false;
    }

    [ByRefEvent]
    public struct EntityChatGetRecipientsEvent
    {
        public readonly EntityChat Chat;

        public EntityChatGetRecipientsEvent(EntityChat chat)
        {
            Chat = chat;
        }

        public bool Handled;
    }

    [ByRefEvent]
    public struct EntityChatTransformEvent
    {
        public EntityChat Chat;

        public EntityChatTransformEvent(EntityChat chat)
        {
            Chat = chat;
        }

        public bool Handled;
    }

    [ByRefEvent]
    public struct BeforeEntityChatEvent
    {
        public readonly EntityChat Chat;

        public BeforeEntityChatEvent(EntityChat chat)
        {
            Chat = chat;
        }

        public bool Cancelled { get; private set; }
        public void Cancel() => Cancelled = true;
        public void Uncancel() => Cancelled = false;
    }

    [ByRefEvent]
    public sealed class GotEntityChatTransformEvent
    {
        public readonly EntityUid Receiver;
        public EntityChat Chat;
        public object? RecipientData;

        public GotEntityChatTransformEvent(EntityUid receiver, EntityChat chat, object? recipientData = null)
        {
            Receiver = receiver;
            Chat = chat;
            RecipientData = recipientData;
        }

        public bool Handled;
    }

    [ByRefEvent]
    public sealed class GotEntityChatEvent
    {
        public readonly EntityUid Receiver;
        public readonly EntityChat Chat;
        public readonly object? RecipientData;

        public GotEntityChatEvent(EntityUid receiver, EntityChat chat, object? recipientData = null)
        {
            Receiver = receiver;
            Chat = chat;
            RecipientData = recipientData;
        }

        public bool Handled;
    }
}
