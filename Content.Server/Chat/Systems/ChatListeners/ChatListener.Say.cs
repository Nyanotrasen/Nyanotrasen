using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Utility;
using Content.Shared.Chat;
using Content.Shared.Speech;

namespace Content.Server.Chat.Systems
{
    public enum ChatDataSay : int
    {
        /// <summary>
        /// The speaker behind this message.
        /// </summary>
        /// <remarks>
        /// This is for situations where an entity is relaying a chat from
        /// somewhere else, like someone talking on a television or radio
        /// object in a room.
        /// </remarks>
        RelayedSpeaker,

        /// <summary>
        /// A flag to notify other ChatListeners that this chat is a spoken message.
        /// </summary>
        /// <remarks>
        /// As opposed to a telepathic or visual message.
        /// </remarks>
        IsSpoken,

        /// <summary>
        /// The string of how the sender is perceived by default.
        /// </summary>
        Identity,

        /// <summary>
        /// The maximum range for this message.
        /// </summary>
        Range,
    }

    public enum ChatRecipientDataSay : int
    {
        /// <summary>
        /// A recipient-specific override message.
        /// </summary>
        Message,

        /// <summary>
        /// A recipient-specific override message, wrapped for the chat log.
        /// </summary>
        WrappedMessage,

        /// <summary>
        /// The distance from the source.
        /// </summary>
        Distance,

        /// <summary>
        /// The string of how the recipient perceives the sender.
        /// </summary>
        Identity,
    }

    /// <summary>
    /// This is the fallback ChatListener.
    ///
    /// Anything that reaches this point should have failed processing
    /// by any other ChatListener.
    /// </summary>
    public sealed class SayListenerSystem : ChatListenerSystem
    {
        private ISawmill _sawmill = default!;

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        public readonly static int DefaultRange = 10;

        public override void Initialize()
        {
            EnabledListeners = EnabledListener.ParseChat | EnabledListener.GetRecipients | EnabledListener.RecipientTransformChat | EnabledListener.Chat;

            base.Initialize();

            _sawmill = Logger.GetSawmill("chat.say");
        }

        public override void OnParseChat(ref EntityChatParseEvent args)
        {
            if (args.Handled)
                return;

            args.Chat.Channel = ChatChannel.Local;
            args.Chat.ClaimedBy = this.GetType();
            args.Chat.SetData(ChatDataSay.IsSpoken, true);

            args.Handled = true;
        }

        public override void OnGetRecipients(ref EntityChatGetRecipientsEvent args)
        {
            if (args.Handled || args.Chat.ClaimedBy != this.GetType())
                return;

            var range = DefaultRange;

            if (TryComp<SpeechComponent>(args.Chat.Source, out var speechComponent))
                range = speechComponent.SpeechRange;

            args.Chat.SetData(ChatDataSay.Range, range);

            var enumerator = new PlayerEntityInRangeEnumerator(EntityManager, _playerManager, args.Chat.Source, range);

            while (enumerator.MoveNext(out var playerEntity, out var distance))
            {
                var recipientData = new EntityChatData();
                recipientData.SetData(ChatRecipientDataSay.Distance, distance);
                args.Chat.Recipients.TryAdd(playerEntity, recipientData);
            }

            args.Handled = true;
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            if (args.Chat.TryGetData<EntityUid>(ChatDataSay.RelayedSpeaker, out var relayedSpeaker))
            {
                // TODO: make better
                args.RecipientData.SetData(ChatRecipientDataSay.Identity, $"{Name(args.Chat.Source)} ({Name(relayedSpeaker)})");
            }

            if (args.Chat.ClaimedBy != this.GetType())
                return;

            var identity = _chatSystem.GetVisibleVoiceIdentity(args.Chat, args.RecipientData, args.Recipient);
            args.RecipientData.SetData(ChatRecipientDataSay.Identity, identity);
        }

        public override void OnChat(ref GotEntityChatEvent args)
        {
            if (args.Handled || args.Chat.ClaimedBy != this.GetType())
                return;

            args.Handled = true;

            if (!TryComp<ActorComponent>(args.Recipient, out var actorComponent))
                return;

            var identity = _chatSystem.GetIdentity(args.Chat, args.RecipientData, args.Recipient);
            var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;
            var wrappedMessage = args.RecipientData.GetData<string>(ChatRecipientDataSay.WrappedMessage) ?? Loc.GetString("chat-manager-entity-say-wrap-message",
                ("entityName", identity),
                ("message", FormattedMessage.EscapeText(message)));

            _chatManager.ChatMessageToOne(args.Chat.Channel,
                message,
                wrappedMessage,
                args.Chat.Source,
                false, // hideChat,
                actorComponent.PlayerSession.ConnectedClient);
        }
    }

    public sealed partial class ChatSystem
    {
        /// <summary>
        /// Try to send a say message from an entity.
        /// </summary>
        public bool TrySendSay(EntityUid source, string message, EntityUid? speaker = null)
        {
            var chat = new EntityChat(source, message)
            {
                Channel = ChatChannel.Local,
                ClaimedBy = typeof(SayListenerSystem),
            };

            chat.SetData(ChatDataSay.IsSpoken, true);

            if (speaker != null)
                chat.SetData(ChatDataSay.RelayedSpeaker, speaker);

            return TrySendChat(source, chat);
        }
    }
}
