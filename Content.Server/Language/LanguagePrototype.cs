using Robust.Shared.Prototypes;

namespace Content.Server.Language
{
    [Prototype("language")]
    public sealed class LanguagePrototype : IPrototype
    {
        [ViewVariables]
        [DataField("name")]
        public string Name { get; set; } = "";

        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        /// Any message that isn't understood by a recipient is passed to this class
        /// for distortion, obfuscation, et cetera.
        /// </summary>
        [ViewVariables]
        [DataField("distorter", serverOnly: true)]
        public UnknownLanguageDistorter? Distorter { get; private set; }
    }
}
