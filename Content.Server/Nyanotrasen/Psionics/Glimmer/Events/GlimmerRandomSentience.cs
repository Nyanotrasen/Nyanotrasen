using System.Linq;
using Content.Server.Ghost.Roles.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Speech.Components;

namespace Content.Server.Psionics.Glimmer;
/// <summary>
/// Glimmer version of the (removed) random sentience event
/// </summary>
public sealed class GlimmerRandomSentience : GlimmerEventSystem
{
    public override string Prototype => "GlimmerRandomSentience";
    public override void Started()
    {
        base.Started();

        var targetList = EntityManager.EntityQuery<SentienceTargetComponent>().ToList();
        RobustRandom.Shuffle(targetList);

        var toMakeSentient = RobustRandom.Next(1, 4);

        foreach (var target in targetList)
        {
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
