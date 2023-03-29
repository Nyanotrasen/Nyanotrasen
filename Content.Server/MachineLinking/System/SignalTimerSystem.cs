using Content.Server.MachineLinking.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.MachineLinking;
using Content.Shared.Examine;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server.MachineLinking.System
{
    public sealed class SignalTimerSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SignalTimerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SignalTimerComponent, ExaminedEvent>(OnExamined);
            // Bound UI subscriptions
            SubscribeLocalEvent<SignalTimerComponent, SignalTimerLengthChangedMessage>(OnSignalTimerLengthChanged);
            SubscribeLocalEvent<SignalTimerComponent, SignalTimerStartedMessage>(OnStart);
            SubscribeLocalEvent<SignalTimerComponent, BeforeActivatableUIOpenEvent>((e,c,_) => DirtyUI(e,c));
        }

        private void OnInit(EntityUid uid, SignalTimerComponent component, ComponentInit args)
        {
            _signalSystem.EnsureTransmitterPorts(uid, component.OnPort, component.OffPort);
        }

        private void OnExamined(EntityUid uid, SignalTimerComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            if (!component.TimerOn)
                return;

            args.PushMarkup(Loc.GetString("signal-timer-examined", ("sec", (int) (component.TargetTime - _gameTiming.CurTime).TotalSeconds)));
        }

        private void OnStart(EntityUid uid, SignalTimerComponent component, SignalTimerStartedMessage args)
        {
            if (component.TimerOn)
                return;

            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            if (TryComp<AccessReaderComponent>(uid, out var access))
            {
                if (!_accessSystem.IsAllowed(player, uid, access))
                {
                    _popup.PopupEntity(Loc.GetString("door-remote-denied"), player, Shared.Popups.PopupType.SmallCaution);
                    return;
                }
            }

            if (!component.TimerOn)
                component.TargetTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Length);

            component.TimerOn = !component.TimerOn;
            _signalSystem.InvokePort(uid, component.TimerOn ? component.OnPort : component.OffPort);
            _audioSystem.PlayPvs(_audioSystem.GetSound(component.ClickSound), uid, AudioParams.Default.WithVariation(0.25f));
            DirtyUI(uid, component);
        }

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
                    DirtyUI(component.Owner, component);
                }
            }
        }

        private void OnSignalTimerLengthChanged(EntityUid uid, SignalTimerComponent component, SignalTimerLengthChangedMessage args)
        {
            if (args.Session.AttachedEntity is not {Valid: true} player)
                return;

            if (TryComp<AccessReaderComponent>(uid, out var access))
            {
                if (!_accessSystem.IsAllowed(player, uid, access))
                {
                    _popup.PopupEntity(Loc.GetString("door-remote-denied"), player, Shared.Popups.PopupType.SmallCaution);
                    return;
                }
            }

            // update component.Length when UI entry is made, and TargetTime only on start.
            component.Length = Math.Clamp(args.Length, 10f, 1200f);
            DirtyUI(uid, component);
        }

        private void DirtyUI(EntityUid uid, SignalTimerComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _userInterfaceSystem.TrySetUiState(uid, SignalTimerUiKey.Key,
                new SignalTimerState(component.TimerOn, component.Length, (float) (component.TargetTime - _gameTiming.CurTime).TotalSeconds));
        }
    }
}
