using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Radio;

namespace Content.Server.MachineLinking.Components
{
    /// <summary>
    ///     This is a modified version of the signal switch.
    /// </summary>

    [RegisterComponent]
    public sealed class SignalTimerComponent : Component
    {
        [DataField("length")]
        public float Length = 10f;

        [DataField("targetTime")]
        public TimeSpan TargetTime;

        /// <summary>
        ///     The port that gets signaled when the switch turns on.
        /// </summary>
        [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string OnPort = "TimerOn";

        /// <summary>
        ///     The port that gets signaled when the switch turns off.
        /// </summary>
        [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string OffPort = "TimerOff";

        [DataField("timerOn")]
        public bool TimerOn;

        [DataField("clickSound")]
        public SoundSpecifier ClickSound { get; set; } = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");

        [DataField("endAlertChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
        public string? EndAlertChannel;

        [DataField("endAlertMessage")]
        public string EndAlertMessage = "signal-timer-component-end-alert-default";
    }
}
