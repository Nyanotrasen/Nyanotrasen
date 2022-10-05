using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class PyrokinesisPowerComponent : Component
    {
        public EntityTargetAction? PyrokinesisPowerAction = null;
    }
}
