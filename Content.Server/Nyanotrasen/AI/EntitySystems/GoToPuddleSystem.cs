using Content.Server.Fluids.Components;

namespace Content.Server.AI.EntitySystems
{
    public sealed class GoToPuddleSystem : EntitySystem
    {
        [Dependency] public EntityLookupSystem _lookup = default!;

        public EntityUid GetNearbyPuddle(EntityUid cleanbot, float range = 10)
        {
            foreach (var entity in EntitySystem.Get<EntityLookupSystem>().GetEntitiesInRange(cleanbot, range))
            {
                if (IoCManager.Resolve<IEntityManager>().HasComponent<PuddleComponent>(entity))
                    return entity;
            }

            return default;
        }
    }
}
