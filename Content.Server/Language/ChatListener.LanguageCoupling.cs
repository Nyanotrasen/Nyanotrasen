using Content.Server.Chat.Systems;
using Content.Shared.Radio;

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

            if (!args.Chat.GetData<bool>(ChatDataSay.IsSpoken))
                return;

            if (!args.Chat.TryGetData<LanguagePrototype>(ChatDataLanguage.Language, out var language))
                return;

            // TODO: WillNeedWrapper flag set by LanguageListener so we don't
            // have to check all these conditions again?

            if (TryComp<LinguisticComponent>(args.Recipient, out var linguisticComponent) &&
                linguisticComponent.CanUnderstand.Contains(language.ID))
            {
                if (linguisticComponent.ChosenLanguage != language)
                {
                    _sawmill.Debug("understands us");

                    if (args.Chat.ClaimedBy == typeof(SayListenerSystem))
                    {
                        args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-manager-entity-say-language-wrap-message",
                            ("entityName", args.Chat.Source),
                            ("message", args.Chat.Message),
                            ("language", language.Name)));
                    }
                    else if (args.Chat.ClaimedBy == typeof(RadioListenerSystem))
                    {
                        var name = args.Chat.Source;
                        var channel = args.RecipientData.GetData<RadioChannelPrototype>(ChatRecipientDataRadio.SharedRadioChannel);
                        var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;

                        if (channel == null)
                        {
                            // Receiving only the whisper part.
                            args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-manager-entity-radio-language-wrap-message",
                                ("entityName", name),
                                ("message", message),
                                ("language", language.Name)));
                        }
                        else
                        {
                            args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-radio-language-message-wrap",
                                ("color", channel.Color),
                                ("channel", $"\\[{channel.LocalizedName}\\]"),
                                ("name", name),
                                ("message", message),
                                ("language", language.Name)));
                        }
                    }
                }

                return;
            }

            if (!args.Chat.TryGetData<string>(ChatDataLanguage.DistortedMessage, out var distortedMessage))
                return;

            _sawmill.Debug("mangles us");

            if (args.Chat.ClaimedBy == typeof(SayListenerSystem))
                args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-manager-entity-say-language-wrap-message",
                    ("language", Loc.GetString("chat-manager-unknown-language")),
                    ("entityName", args.Chat.Source),
                    ("message", distortedMessage)));
            else if (args.Chat.ClaimedBy == typeof(RadioListenerSystem))
            {
                var name = args.Chat.Source;
                var channel = args.RecipientData.GetData<RadioChannelPrototype>(ChatRecipientDataRadio.SharedRadioChannel);
                var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;
                var unknownLanguage = Loc.GetString("chat-manager-unknown-language");

                if (channel == null)
                {
                    // Receiving only the whisper part.
                    args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-manager-entity-radio-language-wrap-message",
                        ("entityName", name),
                        ("message", message),
                        ("language", unknownLanguage)));
                }
                else
                {
                    args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-radio-language-message-wrap",
                        ("color", channel.Color),
                        ("channel", $"\\[{channel.LocalizedName}\\]"),
                        ("name", name),
                        ("message", message),
                        ("language", unknownLanguage)));
                }
            }
        }
    }
}
