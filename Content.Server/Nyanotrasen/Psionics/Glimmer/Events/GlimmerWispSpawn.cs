using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Content.Server.NPC.Components;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Abilities.Psionics;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Psionics.Glimmer;
public sealed class GlimmerWispSpawn : GlimmerEventSystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
    public override string Prototype => "GlimmerWispSpawn";
    private static readonly string WispPrototype = "MobGlimmerWisp";

    public override void Started()
    {
        base.Started();
        var glimmerSources =EntityManager.EntityQuery<GlimmerSourceComponent, TransformComponent>().ToList();
        var normalSpawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        var hiddenSpawnLocations = EntityManager.EntityQuery<MidRoundAntagSpawnLocationComponent, TransformComponent>().ToList();

        var baseCount = Math.Max(1, EntityManager.EntityQuery<PsionicComponent, FactionComponent>().Count() / 10);
        int multiplier = Math.Max(1, (int) _glimmerSystem.GetGlimmerTier() - 2);

        var total = baseCount * multiplier;

        int i = 0;
        while (i < total)
        {
            if (glimmerSources.Count != 0 && _robustRandom.Prob(0.6f))
            {
                EntityManager.SpawnEntity(WispPrototype, _robustRandom.Pick(glimmerSources).Item2.Coordinates);
                i++;
                continue;
            }

            if (normalSpawnLocations.Count != 0)
            {
                EntityManager.SpawnEntity(WispPrototype, _robustRandom.Pick(normalSpawnLocations).Item2.Coordinates);
                i++;
                continue;
            }

            if (hiddenSpawnLocations.Count != 0)
            {
                EntityManager.SpawnEntity(WispPrototype, _robustRandom.Pick(hiddenSpawnLocations).Item2.Coordinates);
                i++;
                continue;
            }
            return;
        }
    }
}
