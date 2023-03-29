using Content.Server.Language;
using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server.Chat.Systems
{
    public enum ChatDataLanguage : int
    {
        /// <summary>
        /// The language being used for this chat.
        /// </summary>
        Language,

        /// <summary>
        /// The message that entities who do not understand the language receive.
        /// </summary>
        DistortedMessage,
    }

    public enum ChatRecipientDataLanguage : int
    {
        /// <summary>
        /// The recipient understands the language used.
        /// </summary>
        IsUnderstood,

        /// <summary>
        /// The recipient is speaking the same language as the speaker.
        /// </summary>
        IsSpeakingSameLanguage,
    }

    public sealed class LanguageListener : ChatListenerSystem
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            ListenBefore = new Type[] { typeof(SayListenerSystem), typeof(WhisperListenerSystem), typeof(RadioListenerSystem) };
            EnabledListeners = EnabledListener.TransformChat | EnabledListener.RecipientTransformChat;

            base.Initialize();

            _sawmill = Logger.GetSawmill("chat.language");
        }

        public override void OnTransformChat(ref EntityChatTransformEvent args)
        {
            if (!args.Chat.GetData<bool>(ChatDataSay.IsSpoken))
                return;

            // Hook into the old TransformSpeech event for accents.
            args.Chat.Message = _chatSystem.TransformSpeech(args.Chat.Source, args.Chat.Message);

            // Only set Language from LinguisticComponent if it hasn't been set from somewhere else up the chain.
            if (!args.Chat.HasData(ChatDataLanguage.Language) &&
                TryComp<LinguisticComponent>(args.Chat.Source, out var linguisticComponent) &&
                linguisticComponent.ChosenLanguage != null)
            {
                args.Chat.SetData(ChatDataLanguage.Language, linguisticComponent.ChosenLanguage);
            }
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            if (!args.Chat.GetData<bool>(ChatDataSay.IsSpoken))
                return;

            if (!args.Chat.TryGetData<LanguagePrototype>(ChatDataLanguage.Language, out var language))
                // This chat has no specified language, so there's nothing we should
                // do with it.
                //
                // Having null for language is like using a universal translator.
                // Everyone will understand it.
                return;

            /* if (!TryComp<LinguisticComponent>(args.Receiver, out var linguisticComponent)) */
            /* { */
            /*     // The receiver is beyond direct linguistic comprehension */
            /*     return; */
            /* } */

            if (TryComp<LinguisticComponent>(args.Recipient, out var linguisticComponent) &&
                (linguisticComponent.BypassUnderstanding ||
                linguisticComponent.CanUnderstand.Contains(language.ID)))
            {
                // The recipient understands us, no mangling needed.
                args.RecipientData.SetData(ChatRecipientDataLanguage.IsUnderstood, true);
                args.RecipientData.SetData(ChatRecipientDataLanguage.IsSpeakingSameLanguage, linguisticComponent.ChosenLanguage == language);
                return;
            }

            if (!args.Chat.TryGetData<string>(ChatDataLanguage.DistortedMessage, out var distortedMessage))
            {
                // The distorted version of this message has yet to be
                // generated. It's created only when necessary to save on
                // string manipulation cycles.

                distortedMessage = language.Distorter.Distort(args.Chat.Source, args.Chat.Message);
                args.Chat.SetData(ChatDataLanguage.DistortedMessage, distortedMessage);
            }

            args.RecipientData.SetData(ChatRecipientDataSay.Message, distortedMessage);
        }
    }

    public sealed partial class ChatSystem
    {
        /// <summary>
        /// Try to send a say message from an entity, with a specific language.
        /// </summary>
        public bool TrySendSayWithLanguage(EntityUid source, string message, LanguagePrototype language, EntityUid? speaker = null)
        {
            var chat = new EntityChat(source, message)
            {
                Channel = ChatChannel.Local,
                ClaimedBy = typeof(SayListenerSystem)
            };

            chat.SetData(ChatDataSay.IsSpoken, true);
            chat.SetData(ChatDataLanguage.Language, language);

            if (speaker != null)
                chat.SetData(ChatDataSay.RelayedSpeaker, speaker);

            return TrySendChat(source, chat);
        }

        /// <summary>
        /// Try to send a radio message from an entity, with a specific language.
        /// </summary>
        public bool TrySendRadioWithLanguage(EntityUid source, string message, RadioChannelPrototype[] radioChannels, LanguagePrototype language, EntityUid? speaker = null)
        {
            var chat = new EntityChat(source, message)
            {
                Channel = ChatChannel.Radio,
                ClaimedBy = typeof(RadioListenerSystem)
            };

            chat.SetData(ChatDataSay.IsSpoken, true);
            chat.SetData(ChatDataRadio.RadioChannels, radioChannels);
            chat.SetData(ChatDataLanguage.Language, language);

            if (speaker != null)
                chat.SetData(ChatDataSay.RelayedSpeaker, speaker);

            return TrySendChat(source, chat);
        }
    }
}
