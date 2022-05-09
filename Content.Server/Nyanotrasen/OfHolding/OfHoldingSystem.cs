using Content.Shared.Interaction;

namespace Content.Server.OfHolding
{
    public sealed class OfHoldingSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OfHoldingComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, OfHoldingComponent component, AfterInteractEvent args)
        {
            if (args.Target == null)
                return;

            if (HasComp<OfHoldingComponent>(args.Target))
            {
                EntityManager.SpawnEntity("Singularity", Transform(args.User).Coordinates);

            }
        }
    }
}
