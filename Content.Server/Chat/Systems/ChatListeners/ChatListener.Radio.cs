using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Utility;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server.Chat.Systems
{
    public enum ChatDataRadio : int
    {
        /// <summary>
        /// An array of RadioChannelPrototypes that the message is broadcast upon.
        /// </summary>
        RadioChannels,

        /// <summary>
        /// This is the entity responsible for actually transmitting the message.
        /// </summary>
        RadioSource,
    }

    public enum ChatRecipientDataRadio : int
    {
        /// <summary>
        /// If the recipient has been cleared to hear the message through the proper radio channel.
        /// </summary>
        /// <remarks>
        /// When this is not set or false, recipient will get the obfuscated whisper version.
        /// </remarks>
        WillHearRadio,

        /// <summary>
        /// The radio channel that the receiver and sender share in common.
        /// </summary>
        /// <remarks>
        /// This is to support sending on multiple radio channels.
        /// </remarks>
        SharedRadioChannel,
    }

    /// <summary>
    /// This event is fired to see if an entity can transmit on a set of radio
    /// channels and what radio entity will be responsible for sending the
    /// transmission.
    /// </summary>
    [ByRefEvent]
    public struct CanTransmitOnRadioEvent
    {
        public readonly RadioChannelPrototype[] Channels;
        public readonly string[] StringChannels;

        public CanTransmitOnRadioEvent(RadioChannelPrototype channel)
        {
            Channels = new RadioChannelPrototype[] { channel };
            StringChannels = new string[] { channel.ID };
        }

        public CanTransmitOnRadioEvent(RadioChannelPrototype[] channels)
        {
            Channels = channels;
            StringChannels = (from channel in channels select channel.ID).ToArray();
        }

        public bool CanTransmit;
        public EntityUid? RadioSource;

        public bool Handled;
    }

    public sealed class RadioListenerSystem : ChatListenerSystem
    {
        private ISawmill _sawmill = default!;

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        public readonly static int ObfuscatedRange = WhisperListenerSystem.ObfuscatedRange;
        public readonly static int VoiceRange = SayListenerSystem.VoiceRange;

        public override void Initialize()
        {
            ListenBefore = new Type[] { typeof(SayListenerSystem) };

            base.Initialize();

            _sawmill = Logger.GetSawmill("chat.radio");
        }

        public override void OnParseChat(ref EntityChatParseEvent args)
        {
            if (args.Handled)
                return;

            var result = _chatSystem.TryProccessRadioMessage(args.Chat.Source, args.Chat.Message, out var parsedMessage, out var radioChannel);

            if (radioChannel == null)
                return;

            var canTransmitEvent = new CanTransmitOnRadioEvent(radioChannel);
            RaiseLocalEvent(args.Chat.Source, ref canTransmitEvent);

            args.Chat.Channel = ChatChannel.Radio;
            args.Chat.ClaimedBy = this.GetType();
            args.Chat.Message = parsedMessage;
            args.Chat.SetData(ChatDataSay.IsSpoken, true);

            if (canTransmitEvent.CanTransmit)
                args.Chat.SetData(ChatDataRadio.RadioChannels, new RadioChannelPrototype[] { radioChannel });

            args.Handled = true;
        }

        public override void OnGetRecipients(ref EntityChatGetRecipientsEvent args)
        {
            if (args.Chat.ClaimedBy != this.GetType())
                return;

            if (!args.Chat.TryGetData<RadioChannelPrototype[]>(ChatDataRadio.RadioChannels, out var radioChannels))
            {
                // No radio channels were supplied. The user does not have access to the channels.
                // Just inform everyone around of the attempt using the new "<Player> radios, <message>" wrapper.
                foreach (var (playerEntity, distance) in _chatSystem.GetPlayerEntitiesInRange(args.Chat.Source, VoiceRange))
                {
                    var recipientData = new EntityChatData();
                    recipientData.SetData(ChatRecipientDataSay.Distance, distance);
                    args.Chat.Recipients.TryAdd(playerEntity, recipientData);
                }

                return;
            }

            var attemptEvent = new RadioReceiveAttemptEvent(args.Chat);

            var xforms = GetEntityQuery<TransformComponent>();

            var transformSource = xforms.GetComponent(args.Chat.Source);
            var sourceCoords = transformSource.Coordinates;

            var enumerator = EntityQueryEnumerator<ActiveRadioComponent>();

            while (enumerator.MoveNext(out EntityUid radioEntity, out var radio))
            {
                // TODO map/station/range checks?

                var transformEntity = xforms.GetComponent(radioEntity);

                // NOTE: Will return false for different maps!
                if (!sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance))
                    continue;

                // Support the RadioReceive events for now.
                RaiseLocalEvent(radioEntity, attemptEvent);
                if (attemptEvent.Cancelled)
                {
                    attemptEvent.Uncancel();
                    continue;
                }

                ActorComponent? actorComponent;
                EntityChatData? recipientData;
                EntityUid recipient = radioEntity;

                if (TryComp<ActorComponent>(radioEntity, out actorComponent) ||
                    TryComp<ActorComponent>(Transform(radioEntity).ParentUid, out actorComponent))
                {
                    // We're dealing with a radio that is a player or is on a player,
                    // so that needs to be handled specially.
                    recipient = actorComponent.Owner;

                    if (args.Chat.Recipients.TryGetValue(recipient, out recipientData))
                    {
                        // Some other radio has already added the player to the recipients list.
                        if (!recipientData.TryGetData<bool>(ChatRecipientDataRadio.WillHearRadio, out var willHearRadio) ||
                            !willHearRadio)
                        {
                            // The player hasn't been marked as being able to hear this message yet.
                            // See if this radio is capable.
                            if (GetSharedRadioChannel(radioChannels, radio.Channels, out var playerSharedChannel))
                            {
                                recipientData.SetData(ChatRecipientDataRadio.WillHearRadio, true);
                                recipientData.SetData(ChatRecipientDataRadio.SharedRadioChannel, playerSharedChannel);
                            }
                        }

                        continue;
                    }
                }

                recipientData = new EntityChatData();

                if (GetSharedRadioChannel(radioChannels, radio.Channels, out var sharedChannel))
                {
                    recipientData.SetData(ChatRecipientDataRadio.WillHearRadio, true);
                    recipientData.SetData(ChatRecipientDataRadio.SharedRadioChannel, sharedChannel);
                }
                else if (distance > VoiceRange)
                {
                    // Candidate won't hear the radio or even the whisper.
                    continue;
                }

                recipientData.SetData(ChatRecipientDataSay.Distance, distance);
                args.Chat.Recipients.TryAdd(recipient, recipientData);
            }

            args.Handled = true;
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            if (args.Chat.ClaimedBy != this.GetType())
                return;

            var distance = args.RecipientData.GetData<float>(ChatRecipientDataSay.Distance);
            var willHearRadio = args.RecipientData.GetData<bool>(ChatRecipientDataRadio.WillHearRadio);

            // NOTE: This should take into account speech occlusion when it is available.

            // The following conditions determine if a recipient should be able
            // to clue in to who is actually speaking, with the assumption that
            // if you're whispering into a radio, you're not able to be outed
            // as easily unless they're close enough to get extra details.
            if (willHearRadio)
            {
                if (distance > VoiceRange || args.Recipient == args.Chat.Source)
                {
                    // The recipient is out of voice range and cannot
                    // correlate what the source is saying on the radio, so
                    // they're only getting the voice.
                    //
                    // Or, they are the speaker, in which case, let them see
                    // themselves as others would on the radio.
                    var identity = _chatSystem.GetVoiceIdentity(args.Chat, args.RecipientData, args.Recipient);
                    args.RecipientData.SetData(ChatRecipientDataSay.Identity, identity);
                }
                else
                {
                    // Within voice range and able to hear the specific radio
                    // channel, so strong correlation if this person has a
                    // strange voice.
                    var identity = _chatSystem.GetVisibleVoiceIdentity(args.Chat, args.RecipientData, args.Recipient);
                    args.RecipientData.SetData(ChatRecipientDataSay.Identity, identity);
                }
            }
            else
            {
                if (distance <= ObfuscatedRange)
                {
                    // Can't hear the radio, but can hear the person talking,
                    // so give them a better idea of who this is.
                    var identity = _chatSystem.GetVisibleVoiceIdentity(args.Chat, args.RecipientData, args.Recipient);
                    args.RecipientData.SetData(ChatRecipientDataSay.Identity, identity);
                }
                else
                {
                    // Otherwise, fall back to the same Identity that anything else uses.
                    // And obscure the message.
                    var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;
                    args.RecipientData.SetData(ChatRecipientDataSay.Message, _chatSystem.ObfuscateMessageReadability(message, 0.2f));
                }
            }
        }

        public override void OnChat(ref GotEntityChatEvent args)
        {
            if (args.Chat.ClaimedBy != this.GetType())
                return;

            args.Handled = true;

            bool willHearRadio = args.RecipientData.GetData<bool>(ChatRecipientDataRadio.WillHearRadio);

            if (willHearRadio)
            {
                // Support the RadioReceive events for now.
                var radioEvent = new RadioReceiveEvent(args.Chat, args.RecipientData);
                RaiseLocalEvent(args.Recipient, radioEvent);
            }

            if (!TryComp<ActorComponent>(args.Recipient, out var actorComponent))
                return;

            if (willHearRadio)
            {
                if (!args.RecipientData.TryGetData<RadioChannelPrototype>(ChatRecipientDataRadio.SharedRadioChannel, out var channel))
                    return;

                var identity = _chatSystem.GetIdentity(args.Chat, args.RecipientData, args.Recipient);
                var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;
                var wrappedMessage = args.RecipientData.GetData<string>(ChatRecipientDataSay.WrappedMessage) ?? Loc.GetString("chat-radio-message-wrap",
                    ("color", channel.Color),
                    ("channel", $"\\[{channel.LocalizedName}\\]"),
                    ("name", identity),
                    ("message", FormattedMessage.EscapeText(message)));

                _chatManager.ChatMessageToOne(ChatChannel.Radio,
                    message,
                    wrappedMessage,
                    args.Chat.Source,
                    false, // hideChat,
                    actorComponent.PlayerSession.ConnectedClient);
            }
            else
            {
                var identity = _chatSystem.GetIdentity(args.Chat, args.RecipientData, args.Recipient);
                var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;
                var wrappedMessage = Loc.GetString("chat-manager-entity-radio-wrap-message",
                    ("entityName", identity),
                    ("message", message));

                _chatManager.ChatMessageToOne(ChatChannel.Radio,
                    message,
                    wrappedMessage,
                    args.Chat.Source,
                    false, // hideChat,
                    actorComponent.PlayerSession.ConnectedClient,
                    Color.DarkGray);
            }
        }

        public static bool GetSharedRadioChannel(IEnumerable<RadioChannelPrototype> a, IEnumerable<string> b, [NotNullWhen(true)] out RadioChannelPrototype? radioChannel)
        {
            radioChannel = null;

            foreach (var i in a)
            {
                foreach (var j in b)
                {
                    if (i.ID == j)
                    {
                        radioChannel = i;
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public sealed partial class ChatSystem
    {
        /// <summary>
        /// Try to send a radio message from an entity.
        /// </summary>
        public bool TrySendRadio(EntityUid source, string message, RadioChannelPrototype[] radioChannels, EntityUid? speaker = null)
        {
            var chat = new EntityChat(source, message)
            {
                Channel = ChatChannel.Radio,
                ClaimedBy = typeof(RadioListenerSystem)
            };

            chat.SetData(ChatDataSay.IsSpoken, true);
            chat.SetData(ChatDataRadio.RadioChannels, radioChannels);

            if (speaker != null)
                chat.SetData(ChatDataSay.RelayedSpeaker, speaker);

            return TrySendChat(source, chat);
        }
    }
}
