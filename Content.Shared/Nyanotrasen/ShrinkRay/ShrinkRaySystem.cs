using Robust.Shared.Physics.Dynamics;
using Robust.Shared.GameObjects;

namespace Content.Shared.ShrinkRay
{
    public sealed class ShrinkRaySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShrinkRayProjectileComponent, StartCollideEvent>(OnStartCollide);
        }

        private void OnStartCollide(EntityUid uid, ShrinkRayProjectileComponent component, StartCollideEvent args)
        {
            var shrunken = EnsureComp<SharedShrunkenComponent>(args.OtherFixture.Body.Owner);
        }
    }
}
