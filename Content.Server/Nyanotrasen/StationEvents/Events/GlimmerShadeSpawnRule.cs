using System.Linq;
using Robust.Shared.Random;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.NPC.Components;
using Content.Server.Psionics.Glimmer;
using Content.Shared.Atmos.Miasma;
using Content.Server.StationEvents.Components;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Abilities.Psionics;

namespace Content.Server.StationEvents.Events;

internal sealed class GlimmerShadeSpawnRule : StationEventSystem<GlimmerShadeRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;

    private static readonly string ShadePrototype = "MobShade";

    protected override void Started(EntityUid uid, GlimmerShadeRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);


        var rottingPlaces = EntityManager.EntityQuery<RottingComponent, TransformComponent>().ToList();
        var normalSpawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        var hiddenSpawnLocations = EntityManager.EntityQuery<MidRoundAntagSpawnLocationComponent, TransformComponent>().ToList();

        float chance = 0.15f * (float) _glimmerSystem.GetGlimmerTier();
        int guaranteedSpawns = _robustRandom.Next(1, (int) _glimmerSystem.GetGlimmerTier());

        // Spawn on top of rotting stuff first.
        foreach(var (rot, xform) in rottingPlaces)
        {
            if (_robustRandom.Prob(chance))
            {
                EntityManager.SpawnEntity(ShadePrototype, xform.Coordinates);
            }
        }

        int i = 0;
        while (i < guaranteedSpawns)
        {
            if (normalSpawnLocations.Count != 0)
            {
                EntityManager.SpawnEntity(ShadePrototype, _robustRandom.Pick(normalSpawnLocations).Item2.Coordinates);
                i++;
                continue;
            }

            if (hiddenSpawnLocations.Count != 0)
            {
                EntityManager.SpawnEntity(ShadePrototype, _robustRandom.Pick(hiddenSpawnLocations).Item2.Coordinates);
                i++;
                continue;
            }

            return;
        }
    }
}
