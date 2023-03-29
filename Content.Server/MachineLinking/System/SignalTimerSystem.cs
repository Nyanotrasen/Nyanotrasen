using Content.Server.MachineLinking.Components;
using Content.Server.UserInterface;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Labels;
using Content.Shared.MachineLinking;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;


namespace Content.Server.MachineLinking.System
{
    public sealed class SignalTimerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

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

            uid.GetUIOrNull(SignalTimerUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        private void OnStart(EntityUid uid, SignalTimerComponent component, SignalTimerStartedMessage args)
        {
            {
                if (!component.TimerOn)
                    component.TargetTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Length);

                component.TimerOn = !component.TimerOn;
                _signalSystem.InvokePort(uid, component.TimerOn ? component.OnPort : component.OffPort);
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
                if (component.TimerOn)
                {
                    // check if TargetTime is reached
                    if (_gameTiming.CurTime < component.TargetTime)
                        continue;

                    // open door and reset state, as the timer is done
                    component.TimerOn = !component.TimerOn;
                    _signalSystem.InvokePort(component.Owner, component.TimerOn ? component.OnPort : component.OffPort);
                }
            }
        }

        private void OnSignalTimerLengthChanged(EntityUid uid, SignalTimerComponent component, SignalTimerLengthChangedMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            // update component.Length when UI entry is made, and TargetTime only on start.
            component.Length = args.Length;
            DirtyUI(uid, component);
        }

        private void DirtyUI(EntityUid uid, SignalTimerComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _userInterfaceSystem.TrySetUiState(uid, SignalTimerUiKey.Key,
                new SignalTimerState(component.TimerOn, component.Length));
        }
    }
}
