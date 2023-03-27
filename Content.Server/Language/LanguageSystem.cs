using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Prototypes;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Popups;

namespace Content.Server.Language
{
    public sealed class LanguageSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("chat.language");

            SubscribeLocalEvent<LinguisticComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<LinguisticComponent, ChangeLanguageActionEvent>(OnChangeLanguage);

            SubscribeLocalEvent<ExtraLanguagesComponent, ComponentStartup>(OnExtraLanguages);
        }

        /// <returns>True if the entity can understand the language when spoken.</returns>
        public bool CanEntityUnderstandSpokenLanguage(EntityUid uid, LanguagePrototype? language, LinguisticComponent? component = null)
        {
            // Per how LanguageListener works, a null language is understood by all.
            if (language == null)
                return true;

            if (!Resolve(uid, ref component))
                return false;

            return component.BypassUnderstanding || component.CanUnderstand.Contains(language.ID);
        }

        /// <returns>True if the entity can understand the language when written.</returns>
        public bool CanEntityReadLanguage(EntityUid uid, LanguagePrototype? language, LinguisticComponent? component = null)
            => CanEntityUnderstandSpokenLanguage(uid, language, component);

        public bool TryGetAnyValidSpokenLanguage(EntityUid uid, [NotNullWhen(true)] out LanguagePrototype? language, LinguisticComponent? component = null)
        {
            language = null;

            if (!Resolve(uid, ref component))
                return false;

            foreach (var languageId in component.CanSpeak)
            {
                language = _prototypeManager.Index<LanguagePrototype>(languageId);
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

        private void OnStartup(EntityUid uid, LinguisticComponent component, ComponentStartup args)
        {
            if (component.Started)
                return;

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
                var action = new InstantAction(_prototypeManager.Index<InstantActionPrototype>(component.ChangeLanguageAction));
                _actionsSystem.AddAction(uid, action, null);
            }

            component.Started = true;
        }

        private void OnChangeLanguage(EntityUid uid, LinguisticComponent component, ChangeLanguageActionEvent args)
        {
            if (component.CanSpeak.Count == 0)
            {
                _sawmill.Error($"{ToPrettyString(uid)} tried to change language and has no speakable languages.");
                return;
            }

            var languages = from item in component.CanSpeak select _prototypeManager.Index<LanguagePrototype>(item);
            var orderedLanguages = languages.OrderBy(item => Loc.GetString(item.Name)).ToArray();

            LanguagePrototype? language;

            if (component.ChosenLanguage == null)
            {
                _sawmill.Warning($"{ToPrettyString(uid)} has null ChosenLanguage when changing languages. Taking the first available one.");

                language = orderedLanguages.First();
            }
            else
            {
                var next = 1 + Array.IndexOf(orderedLanguages, component.ChosenLanguage);

                if (next == orderedLanguages.Length)
                    next = 0;

                language = orderedLanguages[next];
            }

            component.ChosenLanguage = language;

            _popupSystem.PopupEntity($"You begin speaking {Loc.GetString(component.ChosenLanguage.Name)}.", uid, uid, PopupType.Medium);
        }

        private void OnExtraLanguages(EntityUid uid, ExtraLanguagesComponent component, ComponentStartup args)
        {
            var linguisticComponent = EnsureComp<LinguisticComponent>(uid);

            foreach (var languageId in component.CanUnderstand)
                AddLanguage(uid, languageId, false, linguisticComponent);

            foreach (var languageId in component.CanSpeak)
                AddLanguage(uid, languageId, true, linguisticComponent);
        }
    }
}
