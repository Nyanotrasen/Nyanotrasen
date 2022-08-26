using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class PsionicComponent : Component
    {
        public ActionType? PsionicAbility = null;
    }
}
