namespace Content.Server.Drone.Components
{
    [RegisterComponent]
    public sealed class DroneComponent : Component
    {
        public float InteractionBlockRange = 2.15f;

        public EntityUid? Spawner = null;
    }
}
