using Content.Server.Chat.Systems;

namespace Content.Server.Language
{
    public sealed class LanguageListener : ChatListenerSystem
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            Before = new Type[] { typeof(SayListenerSystem), typeof(RadioListenerSystem) };

            InitializeListeners();

            _sawmill = Logger.GetSawmill("chat.language");
        }

        public override void OnTransformChat(ref EntityChatTransformEvent args)
        {
            if (args.Chat.Data is not EntityChatSpokenData spokenData)
            {
                _sawmill.Debug($"rejecting chat due to no spoken data");
                return;
            }

            // Hook into the old TransformSpeech event for accents.
            args.Chat.Message = _chatSystem.TransformSpeech(args.Chat.Source, args.Chat.Message);

            if (TryComp<LinguisticComponent>(args.Chat.Source, out var linguisticComponent) &&
                linguisticComponent.ChosenLanguage != null)
            {
                spokenData.Language = linguisticComponent.ChosenLanguage;
            }
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            if (args.Chat.Data is not EntityChatSpokenData spokenData)
                return;

            if (spokenData.Language == null)
                // This chat has no specified language, so there's nothing we should
                // do with it.
                //
                // Having null for language is like using a universal translator.
                // Everyone will understand it.
                return;

            if (args.RecipientData is not EntityChatSpokenRecipientData recipientData)
            {
                _sawmill.Error($"{ToPrettyString(args.Receiver)} received chat from {ToPrettyString(args.Chat.Source)} with spoken data but lacked recipient data.");
                return;
            }

            _sawmill.Debug("here we are");

            if (TryComp<LinguisticComponent>(args.Receiver, out var linguisticComponent) &&
                linguisticComponent.CanUnderstand.Contains(spokenData.Language.ID))
            {
                // The recipient understands us, no mangling needed.
                return;
            }

            _sawmill.Debug("here we are, misunderstood");

            if (spokenData.Language.Distorter == null)
            {
                _sawmill.Error($"Needed to distort a message for language {spokenData.Language.ID} but it has no distorter set.");
                return;
            }

            if (spokenData.DistortedMessage == null)
            {
                // The distorted version of this message has yet to be
                // generated. It's created only when necessary to save on
                // string manipulation cycles.

                spokenData.DistortedMessage = spokenData.Language.Distorter.Distort(args.Chat.Source, args.Chat.Message);
            }

            recipientData.Message = spokenData.DistortedMessage;
            _sawmill.Debug("we have been mangled");
        }
    }
}
