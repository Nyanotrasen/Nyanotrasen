using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Interaction;
using Content.Shared.Standing;

namespace Content.Server.Morgue.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(EntityStorageComponent))]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public sealed class BodyBagEntityStorageComponent : EntityStorageComponent
    {
        protected override bool AddToContents(EntityUid entity)
        {
            if (IoCManager.Resolve<IEntityManager>().HasComponent<SharedBodyComponent>(entity) && !EntitySystem.Get<StandingStateSystem>().IsDown(entity)) return false;
            return base.AddToContents(entity);
        }
    }
}
