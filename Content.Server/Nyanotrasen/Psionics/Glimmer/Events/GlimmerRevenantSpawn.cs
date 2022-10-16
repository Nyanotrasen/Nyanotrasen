namespace Content.Server.Psionics.Glimmer;
public sealed class GlimmerRevenantSpawn : GlimmerEventSystem
{
    public override string Prototype => "GlimmerRevenantSpawn";
    private static readonly string RevenantPrototype = "MobRevenant";

    public override void Started()
    {
        base.Started();

        if (TryFindRandomTile(out _, out _, out _, out var coords))
        {
            Sawmill.Info($"Spawning revenant at {coords}");
            EntityManager.SpawnEntity(RevenantPrototype, coords);
        }
    }
}
