namespace Content.Server.Drone
{
    [RegisterComponent]
    public sealed class DroneMachineComponent : Component
    {
        public List<EntityUid> Drones = new();

        [DataField("maxDrones")]
        public int MaxDrones = 2;
    }
}
