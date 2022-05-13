using Content.Shared.Sound;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Standing
{
    [Friend(typeof(StandingStateSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed class StandingStateComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("downSoundCollection")]
        public SoundSpecifier DownSoundCollection { get; } = new SoundCollectionSpecifier("BodyFall");

        [DataField("standing")]
        public bool Standing { get; set; } = true;

        /// <summary>
        ///     List of fixtures that had their collision mask changed when the entity was downed.
        ///     Required for re-adding the collision mask.
        /// </summary>
        [DataField("changedFixtures")]
        public List<string> ChangedFixtures = new();

        public override ComponentState GetComponentState()
        {
            return new StandingComponentState(Standing);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not StandingComponentState state) return;

            Standing = state.Standing;
        }

        // I'm not calling it StandingStateComponentState
        [Serializable, NetSerializable]
        private sealed class StandingComponentState : ComponentState
        {
            public bool Standing { get; }

            public StandingComponentState(bool standing)
            {
                Standing = standing;
            }
        }
    }
}
