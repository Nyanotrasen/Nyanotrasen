using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent, NetworkedComponent]
    public sealed class PsionicComponent : Component
    {
        public ActionType? PsionicAbility = null;
    }
}
