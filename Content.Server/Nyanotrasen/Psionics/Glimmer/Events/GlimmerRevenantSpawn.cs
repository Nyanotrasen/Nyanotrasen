using Robust.Shared.Random;

namespace Content.Server.Psionics.Glimmer;
public sealed class GlimmerRevenantSpawn : GlimmerEventSystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    public override string Prototype => "GlimmerRevenantSpawn";
    private static readonly string RevenantPrototype = "MobRevenant";

    public override void Started()
    {
        base.Started();
        List<GlimmerSourceComponent> glimmerSources = new();

        foreach (var glimmerSource in EntityQuery<GlimmerSourceComponent>())
        {
            glimmerSources.Add(glimmerSource);
        }

        if (glimmerSources.Count == 0)
            return;

        var coords = Transform(_robustRandom.Pick(glimmerSources).Owner).Coordinates;

        Sawmill.Info($"Spawning revenant at {coords}");
        EntityManager.SpawnEntity(RevenantPrototype, coords);
    }
}
