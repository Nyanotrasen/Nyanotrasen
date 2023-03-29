using Content.Server.Chat.Systems;
using Content.Server.Explosion.Components;
using Content.Server.Language;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed partial class TriggerSystem
    {
        private void InitializeVoice()
        {
            SubscribeLocalEvent<TriggerOnVoiceComponent, ComponentInit>(OnVoiceInit);
            SubscribeLocalEvent<TriggerOnVoiceComponent, ExaminedEvent>(OnVoiceExamine);
            SubscribeLocalEvent<TriggerOnVoiceComponent, GetVerbsEvent<AlternativeVerb>>(OnVoiceGetAltVerbs);
            SubscribeLocalEvent<TriggerOnVoiceComponent, ListenEvent>(OnListen);
        }

        private void OnVoiceInit(EntityUid uid, TriggerOnVoiceComponent component, ComponentInit args)
        {
            if (component.IsListening)
                EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
            else
                RemCompDeferred<ActiveListenerComponent>(uid);
        }

        private void OnListen(EntityUid uid, TriggerOnVoiceComponent component, ListenEvent args)
        {
            var message = args.Chat.Message.Trim();
            LanguagePrototype? language = args.Chat.GetData<LanguagePrototype>(ChatDataLanguage.Language);

            if (component.IsRecording)
            {
                if (message.Length >= component.MinLength || message.Length <= component.MaxLength)
                    FinishRecording(component, args.Chat.Source, args.Chat.Message, language);

                return;
            }

            if (!string.IsNullOrWhiteSpace(component.KeyPhrase) &&
                message.Contains(component.KeyPhrase, StringComparison.InvariantCultureIgnoreCase) &&
                language == component.Language)
            {
                _adminLogger.Add(LogType.Trigger, LogImpact.High,
                        $"A voice-trigger on {ToPrettyString(uid):entity} was triggered by {ToPrettyString(args.Chat.Source):speaker} speaking the key-phrase \"{component.KeyPhrase}\" in language {language?.ID}.");
                Trigger(uid, args.Chat.Source);
            }
        }

        private void OnVoiceGetAltVerbs(EntityUid uid, TriggerOnVoiceComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString(component.IsRecording ? "verb-trigger-voice-record-stop" : "verb-trigger-voice-record"),
                Act = () =>
                {
                    if (component.IsRecording)
                        StopRecording(component);
                    else
                        StartRecording(component, args.User);
                },
                Priority = 1
            });

            if (string.IsNullOrWhiteSpace(component.KeyPhrase))
                return;

            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("verb-trigger-voice-clear"),
                Act = () =>
                {
                    component.KeyPhrase = null;
                    component.IsRecording = false;
                    RemComp<ActiveListenerComponent>(uid);
                }
            });
        }

        public void StartRecording(TriggerOnVoiceComponent component, EntityUid user)
        {
            component.IsRecording = true;
            EnsureComp<ActiveListenerComponent>(component.Owner).Range = component.ListenRange;

            _adminLogger.Add(LogType.Trigger, LogImpact.Low,
                    $"A voice-trigger on {ToPrettyString(component.Owner):entity} has started recording. User: {ToPrettyString(user):user}");

            _popupSystem.PopupEntity(Loc.GetString("popup-trigger-voice-start-recording"), component.Owner);
        }

        public void StopRecording(TriggerOnVoiceComponent component)
        {
            component.IsRecording = false;
            if (string.IsNullOrWhiteSpace(component.KeyPhrase))
                RemComp<ActiveListenerComponent>(component.Owner);

            _popupSystem.PopupEntity(Loc.GetString("popup-trigger-voice-stop-recording"), component.Owner);
        }

        public void FinishRecording(TriggerOnVoiceComponent component, EntityUid source, string message, LanguagePrototype? language = null)
        {
            component.KeyPhrase = message;
            component.Language = language;
            component.IsRecording = false;

            _adminLogger.Add(LogType.Trigger, LogImpact.Low,
                    $"A voice-trigger on {ToPrettyString(component.Owner):entity} has recorded a new keyphrase: \"{component.KeyPhrase}\" in language {component.Language?.ID}. Recorded from {ToPrettyString(source):speaker}");

            _popupSystem.PopupEntity(Loc.GetString("popup-trigger-voice-recorded", ("keyphrase", component.KeyPhrase!)), component.Owner);
        }

        private void OnVoiceExamine(EntityUid uid, TriggerOnVoiceComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (string.IsNullOrWhiteSpace(component.KeyPhrase))
                    args.PushText(Loc.GetString("trigger-voice-uninitialized"));
                else if (_language.CanEntityReadLanguage(args.Examiner, component.Language))
                    args.PushText(Loc.GetString("examine-trigger-voice", ("keyphrase", component.KeyPhrase)));
                else
                    // NOTE: Use distorted writing when available.
                    args.PushText(Loc.GetString("trigger-voice-unknown-language"));
            }
        }
    }
}
