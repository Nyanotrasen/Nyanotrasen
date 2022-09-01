using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class PsionicInvisibilityPowerComponent : Component
    {
        public InstantAction? PsionicInvisibilityPowerAction = null;
    }
}