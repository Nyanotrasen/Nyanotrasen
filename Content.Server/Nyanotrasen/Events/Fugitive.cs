using Content.Server.Abilities.Psionics;
using Content.Server.MobState;
using Content.Server.StationEvents.Components;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Random;
using System.Linq;


namespace Content.Server.StationEvents.Events;
public sealed class Fugitive : StationEventSystem
{
    [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override string Prototype => "Fugitive";

    public override void Started()
    {
        base.Started();
        var spawnLocations = EntityManager.EntityQuery<VentCritterSpawnLocationComponent, TransformComponent>().ToList();
        var spawn = _robustRandom.Pick(spawnLocations);

        Spawn("SpawnPointGhostFugitive", spawn.Item2.Coordinates);
    }
}
