using Content.Server.Abilities.Psionics;
using Content.Server.MobState;
using Content.Server.Fugitive;
using Content.Server.StationEvents.Components;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;
public sealed class VampSpawn : StationEventSystem
{
    [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override string Prototype => "VampSpawn";

    public override void Started()
    {
        base.Started();
        var spawnLocations = EntityManager.EntityQuery<FugitiveSpawnLocationComponent, TransformComponent>().ToList();
        var backupSpawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();

        TransformComponent? spawn = new();

        if (spawnLocations.Count > 0)
        {
            var spawnLoc = _robustRandom.Pick(spawnLocations);
            spawn = spawnLoc.Item2;
        } else if (backupSpawnLocations.Count > 0)
        {
            var spawnLoc = _robustRandom.Pick(backupSpawnLocations);
            spawn = spawnLoc.Item2;
        }

        if (spawn == null)
            return;

        if (spawn.GridUid == null)
        {
            return;
        }

        Spawn("SpawnPointGhostVampSpider", spawn.Coordinates);
    }
}
