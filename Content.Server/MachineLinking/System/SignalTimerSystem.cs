using Content.Server.MachineLinking.Components;
using Content.Server.UserInterface;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Labels;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
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
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalTimerComponent, ActivateInWorldEvent>(OnActivated);
            // Bound UI subscriptions
            SubscribeLocalEvent<SignalTimerComponent, SignalTimerLengthChangedMessage>(OnSignalTimerLengthChanged);
            SubscribeLocalEvent<SignalTimerComponent, SignalTimerStartedMessage>(OnStart);
        }

        private void OnInit(EntityUid uid, SignalTimerComponent component, ComponentInit args)
        {
            _signalSystem.EnsureTransmitterPorts(uid, component.OnPort, component.OffPort);
        }

        private void OnActivated(EntityUid uid, SignalTimerComponent component, ActivateInWorldEvent args)
        {
            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            var ui = component.Owner.GetUIOrNull(SignalTimerUiKey.Key);
            if (ui == null)
            {
                Logger.Debug($"UI not found for entity with UID {uid}");
            }
            else
            {
                Logger.Debug($"Open UI {ui} for UID {uid}");
                ui.Open(actor.PlayerSession);
            }

            // component.Owner.GetUIOrNull(SignalTimerUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnStart(EntityUid uid, SignalTimerComponent component, SignalTimerStartedMessage args)
        {
            {
                if (!component.State)
                    component.TimeRemaining = component.Length;

                component.State = !component.State;
                _signalSystem.InvokePort(uid, component.State ? component.OnPort : component.OffPort);
                SoundSystem.Play(component.ClickSound.GetSound(), Filter.Pvs(component.Owner), component.Owner,
                    AudioHelpers.WithVariation(0.125f).WithVolume(8f));
            }
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

        private void OnSignalTimerLengthChanged(EntityUid uid, SignalTimerComponent component, SignalTimerLengthChangedMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            // update component.Length AND component.TimeRemaining when UI entry is made?
            component.Length = args.Length;
            component.TimeRemaining = args.Length;
            DirtyUI(uid, component);
        }

        private void DirtyUI(EntityUid uid, SignalTimerComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _userInterfaceSystem.TrySetUiState(uid, SignalTimerUiKey.Key,
                new SignalTimerState(component.State, component.Length));
        }
    }
}
