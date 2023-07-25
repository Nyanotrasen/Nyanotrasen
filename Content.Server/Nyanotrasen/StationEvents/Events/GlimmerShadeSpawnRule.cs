using System.Linq;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Atmos.Miasma;
using Content.Server.StationEvents.Components;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.StationEvents.Events;

internal sealed class GlimmerShadeSpawnRule : StationEventSystem<GlimmerShadeSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;

    private static readonly string ShadePrototype = "MobShade";

    protected override void Started(EntityUid uid, GlimmerShadeSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var rottingPlaces = EntityManager.EntityQuery<RottingComponent, TransformComponent>();
        var normalSpawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList().ConvertAll(item => item.Item2.Coordinates);
        var hiddenSpawnLocations = EntityManager.EntityQuery<MidRoundAntagSpawnLocationComponent, TransformComponent>().ToList().ConvertAll(item => item.Item2.Coordinates);

        var spawnLocations = normalSpawnLocations.ToHashSet();
        spawnLocations.UnionWith(hiddenSpawnLocations.ToHashSet());

        if (spawnLocations.Count == 0)
            return;

        int guaranteedSpawns = _robustRandom.Next(1, (int) _glimmerSystem.GetGlimmerTier() + 1);

        int i = 0;
        while (i < guaranteedSpawns)
        {
            Spawn(ShadePrototype, _robustRandom.Pick(spawnLocations));
            i++;
            continue;
        }

        float rottingSpawnChance = 0.15f * (float) _glimmerSystem.GetGlimmerTier();

        // Spawn on top of rotting stuff
        foreach(var (rot, xform) in rottingPlaces)
        {
            if (_robustRandom.Prob(rottingSpawnChance))
            {
                Spawn(ShadePrototype, xform.Coordinates);
            }
        }

    }
}
