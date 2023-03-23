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
            ListenAfter = new Type[] { typeof(LanguageListener) };

            base.Initialize();

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

            var name = args.Chat.Source;
            var message = args.RecipientData.GetData<string>(ChatRecipientDataSay.Message) ?? args.Chat.Message;

            if (args.RecipientData.GetData<bool>(ChatRecipientDataLanguage.IsUnderstood))
            {
                if (args.RecipientData.GetData<bool>(ChatRecipientDataLanguage.IsSpeakingSameLanguage))
                    return;

                if (args.Chat.ClaimedBy == typeof(SayListenerSystem))
                {
                    args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-manager-entity-say-language-wrap-message",
                        ("entityName", name),
                        ("message", message),
                        ("language", language.Name)));
                }
                else if (args.Chat.ClaimedBy == typeof(WhisperListenerSystem))
                {
                    args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-manager-entity-whisper-language-wrap-message",
                        ("entityName", name),
                        ("message", message),
                        ("language", language.Name)));
                }
                else if (args.Chat.ClaimedBy == typeof(RadioListenerSystem))
                {
                    var channel = args.RecipientData.GetData<RadioChannelPrototype>(ChatRecipientDataRadio.SharedRadioChannel);

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

                return;
            }

            var unknownLanguage = Loc.GetString("chat-manager-unknown-language");

            if (args.Chat.ClaimedBy == typeof(SayListenerSystem))
            {
                args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-manager-entity-say-language-wrap-message",
                    ("entityName", name),
                    ("message", message),
                    ("language", unknownLanguage)));
            }
            else if (args.Chat.ClaimedBy == typeof(WhisperListenerSystem))
            {
                args.RecipientData.SetData(ChatRecipientDataSay.WrappedMessage, Loc.GetString("chat-manager-entity-whisper-language-wrap-message",
                    ("entityName", name),
                    ("message", message),
                    ("language", unknownLanguage)));
            }
            else if (args.Chat.ClaimedBy == typeof(RadioListenerSystem))
            {
                var channel = args.RecipientData.GetData<RadioChannelPrototype>(ChatRecipientDataRadio.SharedRadioChannel);

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
