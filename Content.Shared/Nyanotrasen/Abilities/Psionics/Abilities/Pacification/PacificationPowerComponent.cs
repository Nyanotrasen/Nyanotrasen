using Content.Shared.Actions.ActionTypes;

namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class PacificationPowerComponent : Component
    {
        [DataField("pacifyTime")]
        public TimeSpan PacifyTime = TimeSpan.FromSeconds(20);
        public EntityTargetAction? PacificationPowerAction = null;
    }
}