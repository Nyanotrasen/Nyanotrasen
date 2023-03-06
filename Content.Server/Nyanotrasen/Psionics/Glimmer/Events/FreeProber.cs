using Content.Server.Construction;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Psionics.Glimmer;
using Content.Server.Station.Systems;
using Content.Server.Power.Components;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Server.Psionics.Glimmer;
public sealed class FreeProberSpawn : GlimmerEventSystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    public override string Prototype => "FreeProber";
    private static readonly string ProberPrototype = "GlimmerProber";

    public override void Started()
    {
        base.Started();
        List<EntityUid> PossibleSpawns = new();

        foreach (var glimmerSource in EntityQuery<GlimmerSourceComponent>())
        {
            if (glimmerSource.AddToGlimmer && glimmerSource.Active)
            {
                PossibleSpawns.Add(glimmerSource.Owner);
            }
        }

        if (PossibleSpawns.Count == 0 || _glimmerSystem.Glimmer >= 500 || _robustRandom.Prob(0.25f))
        {
            foreach (var battery in EntityQuery<PowerNetworkBatteryComponent>())
            {
                PossibleSpawns.Add(battery.Owner);
            }
        }

        if (PossibleSpawns.Count > 0)
        {
            _robustRandom.Shuffle(PossibleSpawns);
            foreach (var source in PossibleSpawns)
            {
                if (!TryComp<PhysicsComponent>(source, out var physics))
                    continue;

                if (_stationSystem.GetOwningStation(source) == null)
                    continue;

                for (var i = 0; i < 4; i++)
                {
                    var direction = (DirectionFlag) (1 << i);
                    var coords = Transform(source).Coordinates.Offset(direction.AsDir().ToVec());
                    coords.SnapToGrid();

                    if (!_anchorable.TileFree(coords, physics)) continue;

                    Spawn(ProberPrototype, coords);
                    return;
                }
            }
        }
    }
}
