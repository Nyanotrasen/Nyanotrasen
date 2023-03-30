using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Utility;
using Content.Shared.Chat;
using Content.Shared.Emoting;

namespace Content.Server.Chat.Systems
{
    /// <summary>
    /// </summary>
    public sealed class EmoteListenerSystem : ChatListenerSystem
    {
        private ISawmill _sawmill = default!;

        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public readonly static int DefaultRange = SayListenerSystem.DefaultRange;

        public override void Initialize()
        {
            ListenBefore = new Type[] { typeof(SayListenerSystem) };
            EnabledListeners = EnabledListener.GetRecipients | EnabledListener.Chat;

            base.Initialize();

            _sawmill = Logger.GetSawmill("chat.emote");
        }

        public override void OnGetRecipients(ref EntityChatGetRecipientsEvent args)
        {
            if (args.Handled || args.Chat.ClaimedBy != this.GetType())
                return;

            var range = DefaultRange;

            if (TryComp<EmotingComponent>(args.Chat.Source, out var emotingComponent))
                range = emotingComponent.EmoteRange;

            var enumerator = new PlayerEntityInRangeEnumerator(EntityManager, _playerManager, args.Chat.Source, range);

            while (enumerator.MoveNext(out var playerEntity, out var distance))
            {
                var recipientData = new EntityChatData();
                recipientData.SetData(ChatRecipientDataSay.Distance, distance);
                args.Chat.Recipients.TryAdd(playerEntity, recipientData);
            }

            args.Handled = true;
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
            var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
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
        /// Try to send an emote message from an entity.
        /// </summary>
        public bool TrySendEmote(EntityUid source, string message, EntityUid? speaker = null)
        {
            var chat = new EntityChat(source, message)
            {
                Channel = ChatChannel.Emotes,
                ClaimedBy = typeof(EmoteListenerSystem),
            };

            if (speaker != null)
                chat.SetData(ChatDataSay.RelayedSpeaker, speaker);

            return TrySendChat(source, chat);
        }
    }
}
