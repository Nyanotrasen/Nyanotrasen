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

        float rottingSpawnChance = 0.15f * (float) _glimmerSystem.GetGlimmerTier();

        // Spawn on top of rotting stuff, chance based
        foreach(var (rot, xform) in rottingPlaces)
        {
            if (_robustRandom.Prob(rottingSpawnChance))
            {
                Spawn(ShadePrototype, xform.Coordinates);
            }
        }

        // Normal spawns, somewhat guaranteed
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().Select(item => item.Item2.Coordinates).ToHashSet();
        var hiddenSpawnLocations = EntityManager.EntityQuery<MidRoundAntagSpawnLocationComponent, TransformComponent>().Select(item => item.Item2.Coordinates).ToHashSet();

        spawnLocations.UnionWith(hiddenSpawnLocations);

        if (spawnLocations.Count == 0)
            return;

        int guaranteedSpawns = _robustRandom.Next(1, (int) _glimmerSystem.GetGlimmerTier() + 1);

        while (guaranteedSpawns > 0)
        {
            Spawn(ShadePrototype, _robustRandom.Pick(spawnLocations));
            guaranteedSpawns--;
        }
    }
}
