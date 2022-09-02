using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Doors.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Popups;

namespace Content.Server.Doors.Systems
{
    public sealed class FirelockSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
        [Dependency] private readonly AtmosAlarmableSystem _atmosAlarmable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FirelockComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorDeniedEvent>(OnBeforeDoorDenied);
            SubscribeLocalEvent<FirelockComponent, DoorGetPryTimeModifierEvent>(OnDoorGetPryTimeModifier);
            SubscribeLocalEvent<FirelockComponent, BeforeDoorPryEvent>(OnBeforeDoorPry);
            SubscribeLocalEvent<FirelockComponent, DoorStateChangedEvent>(OnUpdateState);

            SubscribeLocalEvent<FirelockComponent, BeforeDoorAutoCloseEvent>(OnBeforeDoorAutoclose);
            SubscribeLocalEvent<FirelockComponent, AtmosAlarmEvent>(OnAtmosAlarm);
        }

        private void OnBeforeDoorOpened(EntityUid uid, FirelockComponent component, BeforeDoorOpenedEvent args)
        {
            if (component.IsHoldingFire() || component.IsHoldingPressure())
                args.Cancel();
        }

        private void OnBeforeDoorDenied(EntityUid uid, FirelockComponent component, BeforeDoorDeniedEvent args)
        {
            args.Cancel();
        }

        private void OnDoorGetPryTimeModifier(EntityUid uid, FirelockComponent component, DoorGetPryTimeModifierEvent args)
        {
            if (component.IsHoldingFire() || component.IsHoldingPressure())
                args.PryTimeModifier *= component.LockedPryTimeModifier;
        }

        private void OnBeforeDoorPry(EntityUid uid, FirelockComponent component, BeforeDoorPryEvent args)
        {
            if (!TryComp<DoorComponent>(uid, out var door) || door.State != DoorState.Closed)
            {
                return;
            }

            if (component.IsHoldingPressure())
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("firelock-component-is-holding-pressure-message"));
            }
            else if (component.IsHoldingFire())
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("firelock-component-is-holding-fire-message"));
            }
        }

        private void OnUpdateState(EntityUid uid, FirelockComponent component, DoorStateChangedEvent args)
        {
            var ev = new BeforeDoorAutoCloseEvent();
            RaiseLocalEvent(uid, ev);
            if (ev.Cancelled)
            {
                return;
            }

            _doorSystem.SetNextStateChange(uid, component.AutocloseDelay);
        }

        private void OnBeforeDoorAutoclose(EntityUid uid, FirelockComponent component, BeforeDoorAutoCloseEvent args)
        {
            if (!this.IsPowered(uid, EntityManager))
                args.Cancel();

            // Make firelocks autoclose, but only if the last alarm type it
            // remembers was a danger. This is to prevent people from
            // flooding hallways with endless bad air/fire.
            if (_atmosAlarmable.TryGetHighestAlert(uid, out var alarm) && alarm != AtmosAlarmType.Danger || alarm == null)
                args.Cancel();
        }

        private void OnAtmosAlarm(EntityUid uid, FirelockComponent component, AtmosAlarmEvent args)
        {
            if (!TryComp<DoorComponent>(uid, out var doorComponent)) return;

            if (args.AlarmType == AtmosAlarmType.Normal)
            {
                if (doorComponent.State == DoorState.Closed)
                    _doorSystem.TryOpen(uid);
            }
            else if (args.AlarmType == AtmosAlarmType.Danger)
            {
                component.EmergencyPressureStop();
            }
        }
    }
}
