using System.Linq;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Bed.Sleep;
using Content.Shared.Drugs;
using Content.Shared.Chat;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Popups;
using Content.Server.Administration.Managers;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;

namespace Content.Server.Chat.Systems
{
    public enum ChatRecipientDataTelepathic : int
    {
        /// <summary>
        /// If true, the recipient will receive an obfuscated message.
        /// </summary>
        IsDreamer,

        /// <summary>
        /// If true, the recipient will be able to see who sent the original message.
        /// </summary>
        CanSenseSource,
    }

    /// <summary>
    /// This system handles the game-wide telepathy.
    /// </summary>
    public sealed class TelepathicListenerSystem : ChatListenerSystem
    {
        private ISawmill _sawmill = default!;

        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            EnabledListeners = EnabledListener.ChatAttempt | EnabledListener.GetRecipients | EnabledListener.AfterTransform | EnabledListener.RecipientTransformChat | EnabledListener.Chat;

            base.Initialize();

            _sawmill = Logger.GetSawmill("chat.telepathic");
        }
        private IEnumerable<INetChannel> GetAdminClients()
        {
            return _adminManager.ActiveAdmins
                .Select(p => p.ConnectedClient);
        }

        public override void OnChatAttempt(ref EntityChatAttemptEvent args)
        {
            if (args.Chat.ClaimedBy != this.GetType())
                return;

            if (HasComp<PsionicInsulationComponent>(args.Chat.Source) ||
                HasComp<PsionicsDisabledComponent>(args.Chat.Source))
            {
                args.Cancelled = true;

                _popupSystem.PopupEntity(Loc.GetString("chat-manager-send-telepathic-chat-failure-insulated-message"),
                    args.Chat.Source,
                    args.Chat.Source);
            }
        }

        public override void OnGetRecipients(ref EntityChatGetRecipientsEvent args)
        {
            if (args.Handled || args.Chat.ClaimedBy != this.GetType())
                return;

            // Maybe increase glimmer.
            if (_random.Prob(0.1f))
                _glimmerSystem.Glimmer++;

            // Maybe include the dreamers.
            bool getDreamers = _random.Prob(Math.Min(0.33f + ((float) _glimmerSystem.Glimmer / 1500), 1));

            // Add all telepaths.
            var psionicEnumerator = new ReceptiveTelepathicEntityEnumerator(EntityManager, getDreamers);

            while (psionicEnumerator.MoveNext(out EntityUid telepath, out bool isDreamer))
            {
                var recipientData = new EntityChatData();
                recipientData.SetData(ChatRecipientDataTelepathic.IsDreamer, isDreamer);
                args.Chat.Recipients.Add(telepath, recipientData);
            }

            args.Handled = true;
        }

        public override void AfterTransform(ref EntityChatAfterTransformEvent args)
        {
            if (args.Chat.ClaimedBy != this.GetType())
                return;

            var message = args.Chat.Message;

            // Relay to telepathic repeaters. It's simpler this way.
            //
            // If for some reason in the future telepathic repeaters need to have individual handling,
            // the ReceptiveTelepathicEntityEnumerator can be expanded.
            var repeaterEnumerator = EntityQueryEnumerator<TelepathicRepeaterComponent>();
            while (repeaterEnumerator.MoveNext(out EntityUid repeater, out var _))
                _chatSystem.TrySendSay(repeater, message);

            // Admins are messaged out-of-band from the normal GetRecipients because they are OOC entities.
            var admins = GetAdminClients();
            var adminMessageWrap = Loc.GetString("chat-manager-send-telepathic-chat-wrap-message-admin",
                ("source", args.Chat.Source), ("message", message));

            _chatManager.ChatMessageToMany(args.Chat.Channel, message, adminMessageWrap, args.Chat.Source, false, true, admins, Color.PaleVioletRed);
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            if (args.Chat.ClaimedBy != this.GetType())
                return;

            if (!args.RecipientData.GetData<bool>(ChatRecipientDataTelepathic.IsDreamer))
                return;

            // Obfuscate only for dreamers based on the glimmer level.
            float obfuscation = (0.25f + (float) _glimmerSystem.Glimmer / 2000);
            var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;
            var obfuscatedMessage = _chatSystem.ObfuscateMessageReadability(message, obfuscation);
            args.RecipientData.SetData(ChatRecipientDataSay.Message, obfuscatedMessage);
        }

        public override void OnChat(ref GotEntityChatEvent args)
        {
            if (args.Handled || args.Chat.ClaimedBy != this.GetType())
                return;

            args.Handled = true;

            if (!TryComp<ActorComponent>(args.Recipient, out var actorComponent))
                return;

            var message = FormattedMessage.EscapeText(args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message);
            var wrappedMessage = Loc.GetString("chat-manager-send-telepathic-chat-wrap-message",
                ("telepathicChannelName", Loc.GetString("chat-manager-telepathic-channel-name")), ("message", message));

            _chatManager.ChatMessageToOne(args.Chat.Channel,
                message,
                wrappedMessage,
                args.Chat.Source,
                false, // hideChat,
                actorComponent.PlayerSession.ConnectedClient,
                Color.PaleVioletRed);
        }
    }

    public sealed partial class ChatSystem
    {
        /// <summary>
        /// Try to send a telepathic message from an entity.
        /// </summary>
        public bool TrySendTelepathicChat(EntityUid source, string message)
        {
            var chat = new EntityChat(source, message)
            {
                Channel = ChatChannel.Telepathic,
                ClaimedBy = typeof(TelepathicListenerSystem),
            };

            return TrySendChat(source, chat);
        }
    }
}
