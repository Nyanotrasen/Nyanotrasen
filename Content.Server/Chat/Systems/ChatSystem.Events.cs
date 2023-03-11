using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chat;

namespace Content.Server.Chat.Systems
{
    /// <summary>
    /// Arbitrary data container to be filled by ChatListeners.
    /// </summary>
    public class EntityChatData
    {
        private Dictionary<Enum, object> _data = new();

        public bool TryGetData<T>(Enum key, [NotNullWhen(true)] out T value)
        {
            if (_data.TryGetValue(key, out var objValue) &&
                objValue is T)
            {
                value = (T)objValue;
                return true;
            }

            value = default!;
            return false;
        }

        public T? GetData<T>(Enum key)
        {
            if (_data.TryGetValue(key, out var objValue) &&
                objValue is T)
            {
                return (T)objValue;
            }

            return default;
        }

        public bool HasData<T>(Enum key)
        {
            return _data.TryGetValue(key, out var objValue) && objValue is T;
        }

        public bool HasData(Enum key)
        {
            return _data.TryGetValue(key, out var _);
        }

        public void SetData(Enum key, object value)
        {
            _data[key] = value;
        }
    }

    public class EntityChat : EntityChatData
    {
        /// <summary>
        /// The origin of the chat message.
        /// </summary>
        public EntityUid Source;

        /// <summary>
        /// The chat message.
        /// </summary>
        /// <remarks>
        /// May be changed by listeners.
        /// </remarks>
        public string Message;

        /// <summary>
        /// A dictionary of recipients to objects containing arbitrary data.
        /// </summary>
        /// <remarks>
        /// The objects are passed to the recipients at a later stage of processing.
        /// </remarks>
        public Dictionary<EntityUid, EntityChatData> Recipients = new();

        /// <summary>
        /// The designated ChatChannel for this message.
        /// </summary>
        public ChatChannel Channel;

        /// <summary>
        /// Which system, if any, has claimed this chat message.
        /// </summary>
        /// <remarks>
        /// If a system claims a chat, they are saying they will be responsible for sending it.
        /// </remarks>
        public Type? ClaimedBy;

        public EntityChat(EntityUid source, string message)
        {
            Source = source;
            Message = message;
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
    public struct EntityChatAfterTransformEvent
    {
        public readonly EntityChat Chat;

        public EntityChatAfterTransformEvent(EntityChat chat)
        {
            Chat = chat;
        }
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
    public struct GotEntityChatTransformEvent
    {
        public readonly EntityUid Recipient;
        public EntityChat Chat;
        public EntityChatData RecipientData;

        public GotEntityChatTransformEvent(EntityUid recipient, EntityChat chat, EntityChatData recipientData)
        {
            Recipient = recipient;
            Chat = chat;
            RecipientData = recipientData;
        }

        public bool Handled;
    }

    [ByRefEvent]
    public struct GotEntityChatEvent
    {
        public readonly EntityUid Recipient;
        public readonly EntityChat Chat;
        public readonly EntityChatData RecipientData;

        public GotEntityChatEvent(EntityUid recipient, EntityChat chat, EntityChatData recipientData)
        {
            Recipient = recipient;
            Chat = chat;
            RecipientData = recipientData;
        }

        public bool Handled;
    }
}
