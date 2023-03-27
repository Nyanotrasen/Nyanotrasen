using Robust.Shared.GameStates;

namespace Content.Shared.Emoting
{
    [RegisterComponent, NetworkedComponent]
    public sealed class EmotingComponent : Component
    {
        [DataField("enabled"), Access(typeof(EmoteSystem),
             Friend = AccessPermissions.ReadWrite,
             Other = AccessPermissions.Read)] public bool Enabled = true;

        /// <summary>
        /// This is the limit to which someone else can perceive the emoter.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("emoteRange")]
        public int EmoteRange = 10;
    }
}
