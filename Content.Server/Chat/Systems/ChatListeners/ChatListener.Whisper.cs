using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.Chat;
using Content.Shared.IdentityManagement;

namespace Content.Server.Chat.Systems
{
    /// <summary>
    /// This system handles who hears whispers and how whispers are obfuscated.
    /// </summary>
    public sealed class WhisperListenerSystem : ChatListenerSystem
    {
        private ISawmill _sawmill = default!;

        [Dependency] private readonly ChatSystem _chatSystem = default!;

        public readonly static int ObfuscatedRange = 3;

        public override void Initialize()
        {
            base.Initialize();

            Before = new Type[] { typeof(SayListenerSystem) };

            InitializeListeners();

            _sawmill = Logger.GetSawmill("chat.whisper");
        }

        public override void OnGetRecipients(ref EntityChatGetRecipientsEvent args)
        {
            if (args.Handled || args.Chat.ClaimedBy != this.GetType())
                return;

            foreach (var (playerEntity, distance) in _chatSystem.GetPlayerEntitiesInRange(args.Chat.Source, SayListenerSystem.VoiceRange))
            {
                var recipientData = new EntityChatData();
                recipientData.SetData(ChatRecipientDataSay.Distance, distance);
                args.Chat.Recipients.TryAdd(playerEntity, recipientData);
            }

            args.Handled = true;
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            if (args.Chat.ClaimedBy != this.GetType())
                return;

            if (args.RecipientData.TryGetData<float>(ChatRecipientDataSay.Distance, out var distance) &&
                distance <= ObfuscatedRange)
            {
                // Whispers will clue you in if the person isn't who they
                // really seem to be, only if you're close enough to hear them
                // accurately.

                var identity = _chatSystem.GetVisibleVoiceIdentity(args.Chat, args.RecipientData, args.Recipient);
                args.RecipientData.SetData(ChatRecipientDataSay.Identity, identity);

                return;
            }

            var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;
            args.RecipientData.SetData(ChatRecipientDataSay.Message, _chatSystem.ObfuscateMessageReadability(message, 0.2f));
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
            var wrappedMessage = args.RecipientData.GetData<string>(ChatRecipientDataSay.WrappedMessage) ?? Loc.GetString("chat-manager-entity-whisper-wrap-message",
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
        /// Try to send a whisper message from an entity.
        /// </summary>
        public bool TrySendWhisper(EntityUid source, string message, EntityUid? speaker = null)
        {
            var chat = new EntityChat(source, message)
            {
                Channel = ChatChannel.Whisper,
                ClaimedBy = typeof(WhisperListenerSystem),
            };

            chat.SetData(ChatDataSay.IsSpoken, true);

            if (speaker != null)
                chat.SetData(ChatDataSay.RelayedSpeaker, speaker);

            return TrySendChat(source, chat);
        }
    }
}
