using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Utility;
using Content.Shared.Chat;

namespace Content.Server.Chat.Systems
{
    public enum ChatDataSay
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
    }

    public enum ChatRecipientDataSay
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
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeListeners();

            _sawmill = Logger.GetSawmill("chat.say");
        }

        public override void OnParseChat(ref EntityChatParseEvent args)
        {
            if (args.Handled)
                return;

            _sawmill.Debug($"onparsechat: {args.Chat.Message}");

            args.Chat.Channel = ChatChannel.Local;
            args.Chat.ClaimedBy = this.GetType();
            args.Chat.SetData(ChatDataSay.IsSpoken, true);

            // TODO: sanitize

            /* var ev = new EntitySpokeEvent(args.Chat.Source, args.Chat.Message, null, null); */
            /* RaiseLocalEvent(args.Chat.Source, ev, true); */

            args.Handled = true;
        }

        public override void OnGetRecipients(ref EntityChatGetRecipientsEvent args)
        {
            if (args.Handled || args.Chat.ClaimedBy != this.GetType())
                return;

            _sawmill.Debug($"ongetrecipients: {args.Chat.Message}");

            var xforms = GetEntityQuery<TransformComponent>();

            var transformSource = xforms.GetComponent(args.Chat.Source);
            var sourceMapId = transformSource.MapID;
            var sourceCoords = transformSource.Coordinates;

            // TODO: unhardcode

            var voiceRange = 10;

            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} playerEntity)
                    continue;

                var transformEntity = xforms.GetComponent(playerEntity);

                if (transformEntity.MapID != sourceMapId)
                    continue;

                if (sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance) && distance < voiceRange)
                {
                    var recipientData = new EntityChatData();
                    recipientData.SetData(ChatRecipientDataSay.Distance, distance);
                    args.Chat.Recipients.Add(playerEntity, recipientData);
                    continue;
                }
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
        }

        public override void OnChat(ref GotEntityChatEvent args)
        {
            if (args.Handled || args.Chat.ClaimedBy != this.GetType())
                return;

            args.Handled = true;

            _sawmill.Debug($"onchat: {args.Chat.Message}");

            if (!TryComp<ActorComponent>(args.Recipient, out var actorComponent))
                return;

            // TODO: use Identity everywhere

            var identity = args.RecipientData.GetData<string>(ChatRecipientDataSay.Identity) ?? Name(args.Chat.Source);
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
