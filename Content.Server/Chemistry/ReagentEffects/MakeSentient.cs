using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind.Components;
using Content.Server.Speech.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Server.StationEvents.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Psionics;

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
        // No idea what anything past this point does
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole) ||
            entityManager.TryGetComponent(uid, out GhostTakeoverAvailableComponent? takeOver))
        {
            return;
        }

        ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
        entityManager.AddComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);

        entityData.EntityName = Loc.GetString("glimmer-event-awakened-prefix", ("entity", uid));
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }
}
