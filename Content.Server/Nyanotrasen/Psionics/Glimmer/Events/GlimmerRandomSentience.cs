using System.Linq;
using Content.Server.Ghost.Roles.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Speech.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Psionics.Glimmer;
/// <summary>
/// Glimmer version of the (removed) random sentience event
/// </summary>
public sealed class GlimmerRandomSentience : GlimmerEventSystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    public override string Prototype => "GlimmerRandomSentience";
    public override void Started()
    {
        base.Started();

        var targetList = EntityManager.EntityQuery<SentienceTargetComponent>().ToList();
        RobustRandom.Shuffle(targetList);

        var toMakeSentient = RobustRandom.Next(1, 4);

        foreach (var target in targetList)
        {
            if (HasComp<GhostTakeoverAvailableComponent>(target.Owner))
                continue;

            if (!_mobStateSystem.IsAlive(target.Owner))
                continue;

            if (toMakeSentient-- == 0)
                break;

            EntityManager.RemoveComponent<SentienceTargetComponent>(target.Owner);
            MetaData(target.Owner).EntityName = Loc.GetString("glimmer-event-awakened-prefix", ("entity", target.Owner));
            var comp = EntityManager.AddComponent<GhostTakeoverAvailableComponent>(target.Owner);
            comp.RoleName = EntityManager.GetComponent<MetaDataComponent>(target.Owner).EntityName;
            comp.RoleDescription = Loc.GetString("station-event-random-sentience-role-description", ("name", comp.RoleName));
            RemComp<ReplacementAccentComponent>(target.Owner);
            RemComp<MonkeyAccentComponent>(target.Owner);
            EnsureComp<PotentialPsionicComponent>(target.Owner);
        }
    }
}
