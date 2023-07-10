using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Psionics.Glimmer;
using Content.Server.StationEvents.Components;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.StationEvents.Events;

internal sealed class GlimmerRevenantSpawnRule : StationEventSystem<GlimmerRevenantRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;

    private static readonly string RevenantPrototype = "MobRevenant";
    private static readonly string WispPrototype = "MobGlimmerWisp";
    private static readonly string ShadePrototype = "MobShade";

    protected override void Started(EntityUid uid, GlimmerRevenantRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var glimmerSources = EntityManager.EntityQuery<GlimmerSourceComponent, TransformComponent>().ToList();
        var normalSpawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        var hiddenSpawnLocations = EntityManager.EntityQuery<MidRoundAntagSpawnLocationComponent, TransformComponent>().ToList();

        int total = 1;

        if (_glimmerSystem.GetGlimmerTier() == GlimmerTier.Critical)
            total = 2;

        int i = 0;
        while (i < total)
        {
            if (glimmerSources.Count != 0 && _robustRandom.Prob(0.9f))
            {
                SpawnRevenant(_robustRandom.Pick(glimmerSources).Item2.Coordinates);
                i++;
                continue;
            }

            if (normalSpawnLocations.Count != 0)
            {
                SpawnRevenant(_robustRandom.Pick(normalSpawnLocations).Item2.Coordinates);
                i++;
                continue;
            }

            if (hiddenSpawnLocations.Count != 0)
            {
                SpawnRevenant(_robustRandom.Pick(hiddenSpawnLocations).Item2.Coordinates);
                i++;
                continue;
            }
            return;
        }
    }

    // each revenant might bring up to 2 friends...
    private void SpawnRevenant(EntityCoordinates coordinates)
    {
        EntityManager.SpawnEntity(RevenantPrototype, coordinates);

        if (_robustRandom.Prob(0.6f))
        {
            EntityManager.SpawnEntity(ShadePrototype, coordinates);
        }
        if (_robustRandom.Prob(0.3f))
        {
            EntityManager.SpawnEntity(WispPrototype, coordinates);
        }
    }
}
