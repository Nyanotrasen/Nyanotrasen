using Robust.Shared.Prototypes;

namespace Content.Server.Language
{
    [Prototype("unintelligibleSounds")]
    public sealed class UnintelligibleSoundsPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("lines", required: true)]
        public string[] Lines = default!;
    }
}
