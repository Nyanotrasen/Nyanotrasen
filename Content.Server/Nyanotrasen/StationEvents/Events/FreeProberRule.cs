using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Content.Server.Construction;
using Content.Server.Coordinates.Helpers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.Psionics.Glimmer;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.StationEvents.Events;

internal sealed class FreeProberRule : StationEventSystem<FreeProberRuleComponent>
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private static readonly string ProberPrototype = "GlimmerProber";

    protected override void Started(EntityUid uid, FreeProberRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        List<EntityUid> PossibleSpawns = new();

        var query = EntityQueryEnumerator<GlimmerSourceComponent>();
        while (query.MoveNext(out var glimmerSource, out var glimmerSourceComponent))
        {
            if (glimmerSourceComponent.AddToGlimmer && glimmerSourceComponent.Active)
            {
                PossibleSpawns.Add(glimmerSource);
            }
        }

        if (PossibleSpawns.Count == 0 || _glimmerSystem.Glimmer >= 500 || _robustRandom.Prob(0.25f))
        {
            var queryBattery = EntityQueryEnumerator<PowerNetworkBatteryComponent>();
            while (query.MoveNext(out var battery, out var _))
            {
                PossibleSpawns.Add(battery);
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
