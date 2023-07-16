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

        // This makes it so it doesn't affect things that are already sentient
        if (entityManager.HasComponent<MindContainerComponent>(uid))
        {
            return;
        }

        // This piece of code makes things able to speak "normally". One thing of note is that monkeys have a unique accent and won't be affected by this.
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);

        // Monke talk. This makes cognizine a cure to AMIV's long term damage funnily enough, do with this information what you will.
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        entityManager.RemoveComponent<SentienceTargetComponent>(uid);
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
