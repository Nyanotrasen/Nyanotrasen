using Robust.Shared.Physics.Dynamics;
using Content.Shared.Tag;
using Content.Shared.Item;

namespace Content.Shared.ShrinkRay
{
    public sealed class ShrinkRaySystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly IComponentFactory _compFactory = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShrinkRayProjectileComponent, StartCollideEvent>(OnStartCollide);
        }

        private void OnStartCollide(EntityUid uid, ShrinkRayProjectileComponent component, StartCollideEvent args)
        {
            if (_tagSystem.HasAnyTag(args.OtherFixture.Body.Owner, "Structure", "Wall"))
                return;

            var shrunken = EnsureComp<SharedShrunkenComponent>(args.OtherFixture.Body.Owner);

            if (!HasComp<SharedItemComponent>(args.OtherFixture.Body.Owner))
            {
                shrunken.WasOriginallyItem = false;
                var itemComp = (Component) _compFactory.GetComponent("Item");
                itemComp.Owner = args.OtherFixture.Body.Owner;
                EntityManager.AddComponent(args.OtherFixture.Body.Owner, itemComp);
            }
        }
    }
}
