using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Content.Shared.Actions;

namespace Content.Server.Language
{
    [RegisterComponent]
    public sealed class LinguisticComponent : Component
    {
        /// <summary>
        /// What languages can this entity understand when spoken?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canUnderstand", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<LanguagePrototype>))]
        public HashSet<string> CanUnderstand = new();

        /// <summary>
        /// What languages can this entity speak?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canSpeak", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<LanguagePrototype>))]
        public HashSet<string> CanSpeak = new();

        /// <summary>
        /// What is this entity's default chosen language?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("default", customTypeSerializer: typeof(PrototypeIdSerializer<LanguagePrototype>))]
        public string? Default = null;

        /// <summary>
        /// What language has this entity selected to speak (or write)?
        /// </summary>
        [ViewVariables]
        public LanguagePrototype? ChosenLanguage = null;

        [ViewVariables]
        [DataField("dialect", customTypeSerializer: typeof(PrototypeIdSerializer<DialectPrototype>))]
        public string? Dialect = null!;

        // WYCI:
        /* public List<string> CanRead = new(); */
        /* public List<string> CanWrite = new(); */
    }

    public sealed class ChangeLanguageActionEvent : InstantActionEvent { };

    /// <summary>
    /// This is for adding extra languages to a job via AddComponentSpecial.
    /// </summary>
    [RegisterComponent]
    public sealed class ExtraLanguagesComponent : Component
    {
        [DataField("canUnderstand", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<LanguagePrototype>))]
        public HashSet<string> CanUnderstand = new();

        [DataField("canSpeak", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<LanguagePrototype>))]
        public HashSet<string> CanSpeak = new();
    }
}
