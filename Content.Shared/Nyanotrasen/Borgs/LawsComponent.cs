using Robust.Shared.GameStates;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Borgs
{
    [RegisterComponent, NetworkedComponent]
    public sealed class LawsComponent : Component
    {
        [DataField("laws")]
        public HashSet<string> Laws = new HashSet<string>();

        [DataField("canState")]
        public bool CanState = true;

        /// <summary>
        ///     Antispam.
        /// </summary>
        public TimeSpan? StateTime = null;

        [DataField("stateCD")]
        public TimeSpan StateCD = TimeSpan.FromSeconds(30);
    }

    [Serializable, NetSerializable]
    public sealed class LawsComponentState : ComponentState
    {
        public readonly HashSet<string> Laws;

        public LawsComponentState(HashSet<string> laws)
        {
            Laws = laws;
        }
    }
}
