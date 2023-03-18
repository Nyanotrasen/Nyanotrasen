using Content.Server.MachineLinking.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Player;


namespace Content.Server.MachineLinking.System
{
    /// <remarks>
    ///     TODO:
    ///     - Pressing the timer opens its UI instead of just activating it
    ///     - UI needs an entry field for Length and a cancel button?
    ///     - Ability to change component.Length from within the UI.
    ///     - The UI start button should be what activates the timer
    /// </remarks>
    public sealed class SignalTimerSystem : EntitySystem
    {
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalTimerComponent, ActivateInWorldEvent>(OnActivated);
        }

        private void OnInit(EntityUid uid, SignalTimerComponent component, ComponentInit args)
        {
            _signalSystem.EnsureTransmitterPorts(uid, component.OnPort, component.OffPort);
        }

        private void OnActivated(EntityUid uid, SignalTimerComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            // do nothing if the timer is already on, for now. Should actually open the UI and handle things there
            // in a separate function the timer will tick down and then reset itself and the component.State.
            if (component.State)
                return;

            component.State = !component.State;
            _signalSystem.InvokePort(uid, component.State ? component.OnPort : component.OffPort);
            SoundSystem.Play(component.ClickSound.GetSound(), Filter.Pvs(component.Owner), component.Owner,
                AudioHelpers.WithVariation(0.125f).WithVolume(8f));

            args.Handled = true;
        }

        // adapted from MicrowaveSystem
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var component in EntityManager.EntityQuery<SignalTimerComponent>())
            {
                if (component.State)
                {
                    // check if there's still time left
                    component.TimeRemaining -= frameTime;
                    if (component.TimeRemaining > 0)
                        continue;

                    // open door and reset state, as the timer is done
                    component.State = !component.State;
                    _signalSystem.InvokePort(component.Owner, component.State ? component.OnPort : component.OffPort);
                    component.TimeRemaining = component.Length;
                }
            }
        }
    }
}
