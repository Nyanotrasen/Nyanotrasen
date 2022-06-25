using System.Linq;
using Content.Server.Station.Systems;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class BureaucraticError : StationEvent
{
    [Dependency] private readonly IRobustRandom _random = default!;
    public override string StartAnnouncement =>
        Loc.GetString("station-event-bureaucratic-error-announcement");
    public override string Name => "BureaucraticError";

    public override int MinimumPlayers => 25;

    public override float Weight => WeightLow;

    public override int? MaxOccurrences => 2;

    protected override float EndAfter => 1f;

    public override void Startup()
    {
        base.Startup();
        var stationSystem = EntitySystem.Get<StationSystem>();
        var stationJobsSystem = EntitySystem.Get<StationJobsSystem>();
        if (stationSystem.Stations.Count == 0) return; // No stations
        var chosenStation = _random.Pick(stationSystem.Stations.ToList());
        var jobList = stationJobsSystem.GetJobs(chosenStation).Keys.ToList();

        // Low chance to completely change up the late-join landscape by closing all positions except infinite slots.
        // Lower chance than the /tg/ equivalent of this event.
        if (_random.Prob(0.25f))
        {
            var chosenJob = _random.PickAndTake(jobList);
            stationJobsSystem.MakeJobUnlimited(chosenStation, chosenJob); // INFINITE chaos.
            foreach (var job in jobList)
            {
                if (stationJobsSystem.IsJobUnlimited(chosenStation, job))
                    continue;
                stationJobsSystem.TrySetJobSlot(chosenStation, job, 0);
            }
        }
        else
        {
            // Changing every role is maybe a bit too chaotic so instead change 20-30% of them.
            for (var i = 0; i < _random.Next((int)(jobList.Count * 0.20), (int)(jobList.Count * 0.30)); i++)
            {
                var chosenJob = _random.PickAndTake(jobList);
                if (stationJobsSystem.IsJobUnlimited(chosenStation, chosenJob))
                    continue;

                stationJobsSystem.TryAdjustJobSlot(chosenStation, chosenJob, _random.Next(-3, 6));
            }
        }
    }

}
