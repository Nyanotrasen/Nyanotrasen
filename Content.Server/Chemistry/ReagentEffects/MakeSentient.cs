using Content.Server.Mind.Components;
using Content.Server.Speech.Components;
using Content.Server.StationEvents.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Psionics;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed class MakeSentient : ReagentEffect
{
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.SolutionEntity;

        if (entityManager.HasComponent<MindComponent>(uid))
            return;

        entityManager.RemoveComponent<SentienceTargetComponent>(uid);

        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        if (entityManager.HasComponent<GhostTakeoverAvailableComponent>(uid))
            return;

        entityManager.EnsureComponent<PotentialPsionicComponent>(uid);

        var takeOver = entityManager.AddComponent<GhostTakeoverAvailableComponent>(uid);
        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);

        entityData.EntityName = Loc.GetString("glimmer-event-awakened-prefix", ("entity", uid));
        takeOver.RoleName = entityData.EntityName;
        takeOver.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }
}
