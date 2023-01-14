using Content.Server.Station.Systems;
using Content.Server.Abilities.Psionics;
using Content.Shared.Mobs.Systems;
using Content.Server.Psionics;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;
public sealed class NoosphericStorm : StationEventSystem
{
    [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override string Prototype => "NoosphericStorm";

    public override void Started()
    {
        base.Started();
        HashSet<EntityUid> stationsToNotify = new();
        List<PotentialPsionicComponent> validList = new();
        foreach (var psi in EntityManager.EntityQuery<PotentialPsionicComponent>())
        {
            if (_mobStateSystem.IsDead(psi.Owner))
                continue;

            if (HasComp<PsionicComponent>(psi.Owner) || HasComp<PsionicInsulationComponent>(psi.Owner))
                continue;

            validList.Add(psi);
        }
        RobustRandom.Shuffle(validList);

        var mod = 1 + (GetSeverityModifier() / 4);

        var toAwaken = RobustRandom.Next(1, 3);

        // Now we give it to people in the list of living disease carriers earlier
        foreach (var target in validList)
        {
            if (toAwaken-- == 0)
                break;

            _psionicAbilitiesSystem.AddPsionics(target.Owner);

            var station = StationSystem.GetOwningStation(target.Owner);
            if(station == null) continue;
            stationsToNotify.Add((EntityUid) station);
        }

        var glimmerAdded = (int) Math.Round(_robustRandom.Next(65, 85) * mod);

        _glimmerSystem.Glimmer += glimmerAdded;
    }
}
