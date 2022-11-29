using Content.Server.Drone.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Ghost.Roles.Components;

namespace Content.Server.Drone
{
    public sealed class DroneMachineSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneComponent, GhostRoleSpawnerUsedEvent>(OnSpawnerUsed);
            SubscribeLocalEvent<DroneComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<DroneMachineComponent, MapInitEvent>(OnMapInit);
        }

        private void OnSpawnerUsed(EntityUid uid, DroneComponent drone, GhostRoleSpawnerUsedEvent args)
        {
            if (!TryComp<DroneMachineComponent>(args.Spawner, out var machine))
                return;

            machine.Drones.Add(uid);
            drone.Spawner = args.Spawner;
            UpdateDrones(args.Spawner, machine);
        }

        private void OnShutdown(EntityUid uid, DroneComponent component, ComponentShutdown args)
        {
            if (component.Spawner == null)
                return;

            if (!TryComp<DroneMachineComponent>(component.Spawner, out var spawner))
                return;

            spawner.Drones.Remove(uid);
            UpdateDrones(component.Spawner.Value, spawner);
        }

        private void OnMapInit(EntityUid uid, DroneMachineComponent component, MapInitEvent args)
        {
            UpdateDrones(uid, component);
        }

        private void UpdateDrones(EntityUid uid, DroneMachineComponent? component = null, GhostRoleMobSpawnerComponent? spawner = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!Resolve(uid, ref spawner))
                return;

            spawner.CurrentTakeovers = component.Drones.Count;
            spawner.AvailableTakeovers = component.MaxDrones;

            Logger.Error("Updated current takeovers: " + spawner.CurrentTakeovers);
            Logger.Error("Updated available takeovers: " + spawner.AvailableTakeovers);
        }
    }
}
