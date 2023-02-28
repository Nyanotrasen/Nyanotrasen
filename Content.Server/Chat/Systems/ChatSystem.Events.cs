using Content.Server.Language;
using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server.Chat.Systems
{
    public class EntityChat
    {
        /// <summary>
        /// The origin of the chat message.
        /// </summary>
        public EntityUid Source;

        /// <summary>
        /// The original chat message.
        /// </summary>
        public string Message;

        /// <summary>
        /// A dictionary of recipients to arbitrary data objects.
        /// </summary>
        /// <remarks>
        /// The objects are passed to the recipients at a later stage of processing.
        /// </remarks>
        public Dictionary<EntityUid, object> Recipients;

        public ChatChannel Channel;

        /// <summary>
        /// Which system, if any, has claimed this chat message.
        /// </summary>
        /// <remarks>
        /// If a system claims a chat, they are saying they will be responsible for sending it.
        /// </remarks>
        public Type? ClaimedBy;

        /// <summary>
        /// Arbitrary data that may be attached to a chat message.
        /// </summary>
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
        /// <summary>
        /// The radio channels that this chat is being transmitted to.
        /// </summary>
        public RadioChannelPrototype[]? RadioChannels;

        /// <summary>
        /// The language used for this chat.
        /// </summary>
        public LanguagePrototype? Language;

        /// <summary>
        /// A distorted version of the message for anyone who does not
        /// understand the language used.
        /// </summary>
        public string? DistortedMessage;
    }

    public class EntityChatSpokenRecipientData
    {
        /// <summary>
        /// The distance from the source.
        /// </summary>
        public float Distance;

        /// <summary>
        /// A message specifically for this recipient.
        /// </summary>
        public string? Message;

        /// <summary>
        /// A wrapped message specifically for this recipient. This is the
        /// message that will appear in the chat log.
        /// </summary>
        public string? WrappedMessage;

        public string? ObfuscatedMessage;

        public bool WillHearRadio;
        public RadioChannelPrototype? DominantRadio;

        public EntityChatSpokenRecipientData(float distance)
        {
            Distance = distance;
        }
    }

    // New Chat flow:
    //
    // Parse                    - we have no idea what we have
    // Attempt                  - now we know what type of chat this is, do we continue?
    // GetRecipients            - we get who will receive this chat
    // Transform                - we can change what the speaker says
    //
    // BeforeEntityChat         - we could cancel the chat at the recipient
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
        public readonly EntityUid Recipient;
        public readonly EntityChat Chat;

        public BeforeEntityChatEvent(EntityUid recipient, EntityChat chat)
        {
            Recipient = recipient;
            Chat = chat;
        }

        public bool Cancelled { get; private set; }
        public void Cancel() => Cancelled = true;
        public void Uncancel() => Cancelled = false;
    }

    [ByRefEvent]
    public sealed class GotEntityChatTransformEvent
    {
        public readonly EntityUid Recipient;
        public EntityChat Chat;
        public object? RecipientData;

        public GotEntityChatTransformEvent(EntityUid recipient, EntityChat chat, object? recipientData = null)
        {
            Recipient = recipient;
            Chat = chat;
            RecipientData = recipientData;
        }

        public bool Handled;
    }

    [ByRefEvent]
    public sealed class GotEntityChatEvent
    {
        public readonly EntityUid Recipient;
        public readonly EntityChat Chat;
        public readonly object? RecipientData;

        public GotEntityChatEvent(EntityUid recipient, EntityChat chat, object? recipientData = null)
        {
            Recipient = recipient;
            Chat = chat;
            RecipientData = recipientData;
        }

        public bool Handled;
    }
}
