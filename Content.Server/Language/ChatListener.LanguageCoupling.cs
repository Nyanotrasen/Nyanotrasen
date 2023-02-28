using Content.Server.Chat.Systems;

namespace Content.Server.Language
{
    /// <summary>
    /// This listener takes care of any wrapping after the language listener,
    /// then the radio listener, have processed the input.
    ///
    /// The flow goes like: Language -> Say/Radio -> LanguageCoupler
    ///
    /// This allows Language to distort based on language, then Radio to
    /// distort based on distance (what used to look like whispering),
    /// then LanguageCoupler to decide any extra wrapping that needs to be done.
    /// </summary>
    public sealed class LanguageCouplingListener : ChatListenerSystem
    {
        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            After = new Type[] { typeof(LanguageListener) };

            InitializeListeners();

            _sawmill = Logger.GetSawmill("chat.language.coupling");
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            // Within this function, there are checks of ClaimedBy for specific
            // types. This is for determining whether the wrapped message
            // should change, based on the linguistic comprehensibility.
            //
            // This extra bit requires some coupling somewhere, and I decided
            // it made the most sense for the language listeners to know about
            // SayListener et al, rather than the other listeners to explicitly
            // know about Language.

            if (args.Chat.Data is not EntityChatSpokenData spokenData)
                return;

            if (spokenData.Language == null)
                return;

            if (args.RecipientData is not EntityChatSpokenRecipientData recipientData)
                return;

            // TODO: WillNeedWrapper flag set by LanguageListener so we don't
            // have to check all these conditions again?

            if (TryComp<LinguisticComponent>(args.Recipient, out var linguisticComponent) &&
                linguisticComponent.CanUnderstand.Contains(spokenData.Language.ID))
            {
                if (linguisticComponent.ChosenLanguage != spokenData.Language)
                {
                    _sawmill.Debug("understands us");

                    if (args.Chat.ClaimedBy == typeof(SayListenerSystem))
                    {
                        recipientData.WrappedMessage = Loc.GetString("chat-manager-entity-say-language-wrap-message",
                            ("entityName", args.Chat.Source),
                            ("message", args.Chat.Message),
                            ("language", spokenData.Language.Name));
                    }
                    else if (args.Chat.ClaimedBy == typeof(RadioListenerSystem))
                    {
                        var name = args.Chat.Source;
                        var channel = recipientData.DominantRadio;
                        var message = recipientData.Message ?? args.Chat.Message;
                        var language = spokenData.Language.Name;

                        if (channel == null)
                        {
                            // Receiving only the whisper part.
                            recipientData.WrappedMessage = Loc.GetString("chat-manager-entity-radio-language-wrap-message",
                                ("entityName", name),
                                ("message", message),
                                ("language", language));
                        }
                        else
                        {
                            recipientData.WrappedMessage = Loc.GetString("chat-radio-language-message-wrap",
                                ("color", channel.Color),
                                ("channel", $"\\[{channel.LocalizedName}\\]"),
                                ("name", name),
                                ("message", message),
                                ("language", spokenData.Language.Name));
                        }
                    }
                }

                return;
            }

            if (spokenData.DistortedMessage == null)
                return;

            _sawmill.Debug("mangles us");

            if (args.Chat.ClaimedBy == typeof(SayListenerSystem))
                recipientData.WrappedMessage = Loc.GetString("chat-manager-entity-say-language-wrap-message",
                    ("language", Loc.GetString("chat-manager-unknown-language")),
                    ("entityName", args.Chat.Source),
                    ("message", spokenData.DistortedMessage));
            else if (args.Chat.ClaimedBy == typeof(RadioListenerSystem))
            {
                var name = args.Chat.Source;
                var channel = recipientData.DominantRadio;
                var message = recipientData.Message ?? args.Chat.Message;
                var language = Loc.GetString("chat-manager-unknown-language");

                if (channel == null)
                {
                    // Receiving only the whisper part.
                    recipientData.WrappedMessage = Loc.GetString("chat-manager-entity-radio-language-wrap-message",
                        ("entityName", name),
                        ("message", message),
                        ("language", language));
                }
                else
                {
                    recipientData.WrappedMessage = Loc.GetString("chat-radio-language-message-wrap",
                        ("color", channel.Color),
                        ("channel", $"\\[{channel.LocalizedName}\\]"),
                        ("name", name),
                        ("message", message),
                        ("language", language));
                }
            }
        }
    }
}
