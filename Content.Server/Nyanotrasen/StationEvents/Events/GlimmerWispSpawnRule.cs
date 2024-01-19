using System.Linq;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.NPC.Components;
using Content.Server.Psionics.Glimmer;
using Content.Server.StationEvents.Components;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Abilities.Psionics;

namespace Content.Server.StationEvents.Events;

internal sealed class GlimmerWispSpawnRule : StationEventSystem<GlimmerWispSpawnRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;

    private static readonly string WispPrototype = "MobGlimmerWisp";
    private static readonly string ShadePrototype = "MobShade";

    protected override void Started(EntityUid uid, GlimmerWispSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var glimmerSources = EntityManager.EntityQuery<GlimmerSourceComponent, TransformComponent>().ToList();
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().Select(item => item.Item2.Coordinates).ToHashSet();
        var hiddenSpawnLocations = EntityManager.EntityQuery<MidRoundAntagSpawnLocationComponent, TransformComponent>().Select(item => item.Item2.Coordinates).ToHashSet();

        spawnLocations.UnionWith(hiddenSpawnLocations);

        var baseCount = Math.Max(1, EntityManager.EntityQuery<PsionicComponent, NpcFactionMemberComponent>().Count() / 10);
        int multiplier = Math.Max(1, (int) _glimmerSystem.GetGlimmerTier() - 2);

        var total = baseCount * multiplier;

        while (total > 0)
        {
            if (glimmerSources.Count != 0 && _robustRandom.Prob(0.4f))
            {
                SpawnWisp(_robustRandom.Pick(glimmerSources).Item2.Coordinates);
                total--;
                continue;
            }

            if (spawnLocations.Count != 0)
            {
                SpawnWisp(_robustRandom.Pick(spawnLocations));
                total--;
                continue;
            }

            return;
        }
    }

    // each wisp might bring a friend...
    private void SpawnWisp(EntityCoordinates coordinates)
    {
        Spawn(WispPrototype, coordinates);
        if (_robustRandom.Prob(0.2f))
        {
            Spawn(ShadePrototype, coordinates);
        }
    }
}
