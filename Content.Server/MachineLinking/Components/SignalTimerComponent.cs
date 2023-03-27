using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MachineLinking.Components
{
    /// <summary>
    ///     This is a modified version of the signal switch.
    /// </summary>

    [RegisterComponent]
    public sealed class SignalTimerComponent : Component
    {
        [DataField("length")]
        public float Length;

        [DataField("targetTime")]
        public TimeSpan TargetTime;

        /// <summary>
        ///     The port that gets signaled when the switch turns on.
        /// </summary>
        [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string OnPort = "On";

        /// <summary>
        ///     The port that gets signaled when the switch turns off.
        /// </summary>
        [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string OffPort = "Off";

        [DataField("state")]
        public bool State;

        [DataField("clickSound")]
        public SoundSpecifier ClickSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
    }
}
