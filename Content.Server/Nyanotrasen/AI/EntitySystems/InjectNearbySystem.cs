using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.AI.Tracking;
using Content.Shared.MobState.Components;
using Content.Shared.Damage;

namespace Content.Server.AI.EntitySystems
{
    public sealed class InjectNearbySystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;

        public EntityUid GetNearbyInjectable(EntityUid medibot, float range = 4)
        {
            foreach (var entity in EntitySystem.Get<EntityLookupSystem>().GetEntitiesInRange(medibot, range))
            {
                if (HasComp<InjectableSolutionComponent>(entity) && HasComp<MobStateComponent>(entity))
                    return entity;
            }

            return default;
        }

        public bool Inject(EntityUid medibot, EntityUid target)
        {
            if (!TryComp<DamageableComponent>(target, out var damage))
                return false;

            if (!_solutionSystem.TryGetInjectableSolution(target, out var injectable))
                return false;

            if (damage.TotalDamage == 0)
                return false;

            if (damage.TotalDamage <= 50)
            {
                _solutionSystem.TryAddReagent(target, injectable, "Tricordrazine", 15, out var accepted);
                EnsureComp<RecentlyInjectedComponent>(target);
                return true;
            }

            if (damage.TotalDamage >= 100)
            {
                _solutionSystem.TryAddReagent(target, injectable, "Inaprovaline", 15, out var accepted);
                EnsureComp<RecentlyInjectedComponent>(target);
                return true;
            }

            return false;
        }
    }
}
