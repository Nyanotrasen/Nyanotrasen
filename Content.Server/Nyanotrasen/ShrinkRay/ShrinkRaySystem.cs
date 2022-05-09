using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.ShrinkRay;
using Content.Server.Clothing.Components;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.ShrinkRay
{
    public sealed class ShrinkRaySystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;

        [Dependency] private readonly SharedShrinkRaySystem _sharedShrink = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShrinkRayProjectileComponent, StartCollideEvent>(OnStartCollide);
            SubscribeLocalEvent<ShrunkenComponent, ComponentShutdown>(OnShutdown);
        }

        private Queue<EntityUid> RemQueue = new();
        public override void Update(float frameTime)
        {
            base.FrameUpdate(frameTime);
            foreach (var shrunken in RemQueue)
            {
                RemComp<ShrunkenComponent>(shrunken);
                RemComp<ShrunkenSpriteComponent>(shrunken);
                _sharedShrink.RemoveShrunken(shrunken);

            }
            RemQueue.Clear();

            foreach (var shrunken in EntityQuery<ShrunkenComponent>())
            {
                shrunken.Accumulator += frameTime;
                if (shrunken.Accumulator < shrunken.ShrinkTime.TotalSeconds)
                {
                    continue;
                }
                RemQueue.Enqueue(shrunken.Owner);
            }
        }

        private void OnStartCollide(EntityUid uid, ShrinkRayProjectileComponent component, StartCollideEvent args)
        {
            if (_tagSystem.HasAnyTag(args.OtherFixture.Body.Owner, "Structure", "Wall"))
                return;

            if (TryComp<ShrunkenComponent>(args.OtherFixture.Body.Owner, out var alreadyShrank))
            {
                alreadyShrank.Accumulator = 0;
                return;
            }

            EnsureComp<ShrunkenComponent>(args.OtherFixture.Body.Owner);
            EnsureComp<ShrunkenSpriteComponent>(args.OtherFixture.Body.Owner);

            if (!HasComp<ItemComponent>(args.OtherFixture.Body.Owner) && !HasComp<SharedItemComponent>(args.OtherFixture.Body.Owner)) // yes it will crash without both of these
            {
                var shrunken = Comp<ShrunkenComponent>(args.OtherFixture.Body.Owner);
                shrunken.WasOriginallyItem = false;
                AddComp<ItemComponent>(args.OtherFixture.Body.Owner);
            }
        }

        private void OnShutdown(EntityUid uid, ShrunkenComponent component, ComponentShutdown args)
        {
            if (!component.WasOriginallyItem)
                RemComp<ItemComponent>(uid);
        }
    }
}
