using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Prototypes;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Language
{
    public sealed class LanguageSystem : ChatListenerSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeListeners();

            _sawmill = Logger.GetSawmill("chat.language");

            SubscribeLocalEvent<LinguisticComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<LinguisticComponent, ChangeLanguageActionEvent>(OnChangeLanguage);

            SubscribeLocalEvent<ExtraLanguagesComponent, ComponentStartup>(OnExtraLanguages);
        }

        public bool TryGetAnyValidSpokenLanguage(EntityUid uid, [NotNullWhen(true)] out LanguagePrototype? language, LinguisticComponent? component = null)
        {
            language = null;

            if (!Resolve(uid, ref component))
                return false;

            foreach (var languageId in component.CanSpeak)
            {
                if (!_prototypeManager.TryIndex<LanguagePrototype>(languageId, out var candidate))
                {
                    _sawmill.Error($"{ToPrettyString(uid)} has an invalid speakable language: {languageId}");
                    continue;
                }

                language = candidate;
                return true;
            }

            return false;
        }

        public void AddLanguage(EntityUid uid, string languageId, bool canSpeak = false, LinguisticComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!_prototypeManager.HasIndex<LanguagePrototype>(languageId))
            {
                _sawmill.Error($"Tried to add nonexistent language {languageId} to {ToPrettyString(uid)}");
                return;
            }

            component.CanUnderstand.Add(languageId);

            if (canSpeak)
            {
                component.CanSpeak.Add(languageId);

                if (component.CanSpeak.Count == 2)
                {
                    var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>("ChangeLanguage"));
                    _actionsSystem.AddAction(uid, action, null);
                }
            }
        }

        public void RemoveLanguage(EntityUid uid, string languageId, LinguisticComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!_prototypeManager.HasIndex<LanguagePrototype>(languageId))
            {
                _sawmill.Error($"Tried to remove nonexistent language {languageId} from {ToPrettyString(uid)}");
                return;
            }

            component.CanUnderstand.Remove(languageId);
            component.CanSpeak.Remove(languageId);

            if (component.ChosenLanguage?.ID == languageId)
            {
                // Hey, I was speaking that language.
                TryGetAnyValidSpokenLanguage(uid, out var newChosenLanguage, component);
                component.ChosenLanguage = newChosenLanguage;
            }

            if (component.CanSpeak.Count == 1)
            {
                var actionPrototype = _prototypeManager.Index<InstantActionPrototype>("ChangeLanguage");
                _actionsSystem.RemoveAction(uid, actionPrototype, null);
            }
        }

        private void OnInit(EntityUid uid, LinguisticComponent component, ComponentInit args)
        {
            if (component.Default != null)
            {
                if (component.CanSpeak.Contains(component.Default))
                {
                    if (!_prototypeManager.TryIndex<LanguagePrototype>(component.Default, out var language))
                        _sawmill.Error($"{ToPrettyString(uid)} has an invalid default language: {component.Default}");
                    else
                        component.ChosenLanguage = language;
                }
                else
                    _sawmill.Error($"{ToPrettyString(uid)} has default language {component.Default} but cannot speak it.");
            }
            else if (TryGetAnyValidSpokenLanguage(uid, out var language, component))
            {
                component.ChosenLanguage = language;
            }

            if (component.CanSpeak.Count > 1)
            {
                var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>("ChangeLanguage"));
                _actionsSystem.AddAction(uid, action, null);
            }
        }

        private void OnChangeLanguage(EntityUid uid, LinguisticComponent component, ChangeLanguageActionEvent args)
        {
            if (component.CanSpeak.Count == 0)
            {
                _sawmill.Error($"{ToPrettyString(uid)} tried to change language and has no speakable languages.");
                return;
            }

            var list = component.CanSpeak.ToList();
            list.Sort();
            var orderedLanguages = list.ToArray();

            string languageId = default!;

            if (component.ChosenLanguage == null)
            {
                _sawmill.Warning($"{ToPrettyString(uid)} has null ChosenLanguage when changing languages. Taking the first available one.");

                languageId = orderedLanguages.First();
            }
            else
            {
                var next = 0;

                for (int i = 0; i < orderedLanguages.Length; ++i)
                {
                    if (component.ChosenLanguage.ID == orderedLanguages[i])
                        if (1 + i != orderedLanguages.Length)
                        {
                            next = 1 + i;
                            break;
                        }
                }

                languageId = orderedLanguages[next];
            }

            if (!_prototypeManager.TryIndex<LanguagePrototype>(languageId, out var language))
            {
                _sawmill.Error($"{ToPrettyString(uid)} has an invalid speakable language: {languageId}");
                return;
            }

            component.ChosenLanguage = language;

            _popupSystem.PopupEntity($"You begin speaking {component.ChosenLanguage.Name}.", uid, uid);
        }

        private void OnExtraLanguages(EntityUid uid, ExtraLanguagesComponent component, ComponentStartup args)
        {
            var linguisticComponent = EnsureComp<LinguisticComponent>(uid);

            foreach (var languageId in component.CanUnderstand)
                AddLanguage(uid, languageId, false, linguisticComponent);

            foreach (var languageId in component.CanSpeak)
                AddLanguage(uid, languageId, true, linguisticComponent);
        }

        public override void OnTransformChat(ref EntityChatTransformEvent args)
        {
            if (args.Chat.Data is not EntityChatSpokenData spokenData)
                return;

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
            // Within this function, there are checks of ClaimedBy for specific
            // types. This is for determining whether the wrapped message
            // should change, based on the linguistic comprehensibility.
            //
            // This extra bit requires some coupling somewhere, and I decided
            // it made the most sense for LanguageSystem to know about
            // SayListener et al, rather than the other listeners to explicitly
            // know about Language.

            if (args.Chat.Data is not EntityChatSpokenData spokenData)
                return;

            if (spokenData.Language == null)
                // This chat has no specified language, so there's nothing we can
                // do with it.
                //
                // Having null for language is effectively someone speaking in
                // tongues. Everyone will understand it.
                return;

            if (args.RecipientData is not EntityChatSpokenRecipientData recipientData)
            {
                _sawmill.Error($"{ToPrettyString(args.Receiver)} received chat from {ToPrettyString(args.Chat.Source)} with spoken data but lacked recipient data.");
                return;
            }

            if (TryComp<LinguisticComponent>(args.Receiver, out var linguisticComponent) &&
                linguisticComponent.CanUnderstand.Contains(spokenData.Language.ID))
            {
                // The recipient understands us, no mangling needed.

                if (linguisticComponent.ChosenLanguage != spokenData.Language)
                {
                    // But if they're currently speaking a different language,
                    // we should indicate that, to lessen any confusing
                    // scenarios where one person is speaking English and the
                    // other is speaking Mandarin, but they do not realize it,
                    // and a third party speaks only one of these languages.

                    switch (args.Chat.ClaimedBy)
                    {
                        case Type SayListenerSystem:
                            recipientData.WrappedMessage = Loc.GetString("chat-manager-entity-say-language-wrap-message",
                                ("language", spokenData.Language.Name),
                                ("entityName", args.Chat.Source),
                                ("message", args.Chat.Message));
                            break;
                    }
                }

                return;
            }

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

            switch (args.Chat.ClaimedBy)
            {
                case Type SayListenerSystem:
                    recipientData.WrappedMessage = Loc.GetString("chat-manager-entity-say-language-wrap-message",
                        ("language", Loc.GetString("chat-manager-unknown-language")),
                        ("entityName", args.Chat.Source),
                        ("message", spokenData.DistortedMessage));
                break;
            }
        }
    }
}
