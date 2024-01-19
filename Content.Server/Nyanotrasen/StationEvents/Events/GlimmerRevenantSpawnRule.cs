using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Psionics.Glimmer;
using Content.Server.StationEvents.Components;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.StationEvents.Events;

internal sealed class GlimmerRevenantSpawnRule : StationEventSystem<GlimmerRevenantSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;

    private static readonly string RevenantPrototype = "MobRevenant";
    private static readonly string WispPrototype = "MobGlimmerWisp";
    private static readonly string ShadePrototype = "MobShade";

    protected override void Started(EntityUid uid, GlimmerRevenantSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var glimmerSources = EntityManager.EntityQuery<GlimmerSourceComponent, TransformComponent>().ToList();
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().Select(item => item.Item2.Coordinates).ToHashSet();
        var hiddenSpawnLocations = EntityManager.EntityQuery<MidRoundAntagSpawnLocationComponent, TransformComponent>().Select(item => item.Item2.Coordinates).ToHashSet();

        spawnLocations.UnionWith(hiddenSpawnLocations);

        int total = 1;

        if (_glimmerSystem.GetGlimmerTier() == GlimmerTier.Critical)
            total = 2;

        while (total > 0)
        {
            if (glimmerSources.Count != 0 && _robustRandom.Prob(0.9f))
            {
                SpawnRevenant(_robustRandom.Pick(glimmerSources).Item2.Coordinates);
            }
            else if (spawnLocations.Count != 0)
            {
                SpawnRevenant(_robustRandom.Pick(spawnLocations));
            }
            else
            {
                return;
            }
            total--;
        }
    }

    // each revenant might bring up to 2 friends...
    private void SpawnRevenant(EntityCoordinates coordinates)
    {
        Spawn(RevenantPrototype, coordinates);

        if (_robustRandom.Prob(0.6f))
        {
            Spawn(ShadePrototype, coordinates);
        }
        if (_robustRandom.Prob(0.3f))
        {
            Spawn(WispPrototype, coordinates);
        }
    }
}
