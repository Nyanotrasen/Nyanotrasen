using Content.Server.StationEvents.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.StationEvents.Events;
public sealed class MidRoundAntag : StationEventSystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override string Prototype => "MidRoundAntag";

    public readonly IReadOnlyList<string> MidRoundAntags = new[]
    {
        "SpawnPointGhostRatKing", "SpawnPointGhostVampSpider", "SpawnPointGhostFugitive", "MobEvilTwinSpawn"
    };

    public override void Started()
    {
        base.Started();
        var spawnLocations = EntityManager.EntityQuery<MidRoundAntagSpawnLocationComponent, TransformComponent>().ToList();
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

        Spawn(_robustRandom.Pick(MidRoundAntags), spawn.Coordinates);
    }
}
