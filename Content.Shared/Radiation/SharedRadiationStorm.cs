using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation
{
    [NetworkedComponent()]
    public abstract class SharedRadiationPulseComponent : Component
    {
        [DataField("radsPerSecond")]
        public float RadsPerSecond { get; set; } = 1;

        /// <summary>
        /// Radius of the pulse from its position
        /// </summary>
        public virtual float Range { get; set; }

        public virtual bool Decay { get; set; }
        public virtual bool Draw { get; set; }

        public virtual TimeSpan StartTime { get; }
        public virtual TimeSpan EndTime { get; }
    }

    /// <summary>
    /// For syncing the pulse's lifespan between client and server for the overlay
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RadiationPulseState : ComponentState
    {
        // not networking RadsPerSecond because damage is only ever dealt by server-side systems.

        public readonly float Range;
        public readonly bool Draw;
        public readonly bool Decay;
        public readonly TimeSpan StartTime;
        public readonly TimeSpan EndTime;

        public RadiationPulseState(float range, bool draw, bool decay, TimeSpan startTime, TimeSpan endTime)
        {
            Range = range;
            Draw = draw;
            Decay = decay;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
