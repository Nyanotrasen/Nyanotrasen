using Robust.Shared.Prototypes;

namespace Content.Server.Language
{
    [Prototype("dialect")]
    public sealed class DialectPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        [ViewVariables]
        [DataField("lines", required: true)]
        public string[] Lines = default!;
    }
}
