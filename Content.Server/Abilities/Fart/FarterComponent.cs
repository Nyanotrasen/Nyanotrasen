using Content.Shared.Actions.ActionTypes;
using Content.Shared.Sound;

namespace Content.Server.Abilities.Fart
{
    [RegisterComponent]
    public sealed class FarterComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("fartSound")]
        public SoundSpecifier FartSound { get; set; } = new SoundCollectionSpecifier("Fart");

        [DataField("fartAction")]
        public InstantAction FartAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(3),
            Name = "fart-action",
            Description = "fart-action-desc",
            Priority = -1,
            Event = new FartActionEvent(),
        };
    }
}
