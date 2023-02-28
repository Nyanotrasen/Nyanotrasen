using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Utility;
using Content.Shared.Chat;

namespace Content.Server.Chat.Systems
{
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
            args.Chat.Data = new EntityChatSpokenData();

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
                    args.Chat.Recipients.Add(playerEntity, new EntityChatSpokenRecipientData(distance));
                    continue;
                }
            }

            args.Handled = true;
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            if (args.Chat.Data is not EntityChatSpokenData spokenData)
                return;

            if (args.RecipientData is not EntityChatSpokenRecipientData recipientData)
                return;

           if (spokenData.RelayedSpeaker != null)
           {
               // TODO: make better
               recipientData.Identity = $"{Name(args.Chat.Source)} ({Name(spokenData.RelayedSpeaker.Value)})";
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

            if (args.Chat.Data is not EntityChatSpokenData data)
                return;

            if (args.RecipientData is not EntityChatSpokenRecipientData recipientData)
                return;

            // TODO: use Identity everywhere

            // If a message has been designated specifically for this recipient, use that instead.
            var message = recipientData.Message ?? args.Chat.Message;
            var wrappedMessage = recipientData.WrappedMessage ?? Loc.GetString("chat-manager-entity-say-wrap-message",
                ("entityName", recipientData.Identity ?? Name(args.Chat.Source)),
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
                Data = new EntityChatSpokenData()
                {
                    RelayedSpeaker = speaker
                }
            };

            return TrySendChat(source, chat);
        }
    }
}
