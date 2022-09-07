using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class TelegnosisPowerComponent : Component
    {
        [DataField("prototype")]
        public string Prototype = "MobObserverTelegnostic";
        public InstantAction? TelegnosisPowerAction = null;
    }
}