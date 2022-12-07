using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class MetapsionicPowerComponent : Component
    {
        [DataField("range")]
        public float Range = 6f;

        public InstantAction? MetapsionicPowerAction = null;
    }
}
