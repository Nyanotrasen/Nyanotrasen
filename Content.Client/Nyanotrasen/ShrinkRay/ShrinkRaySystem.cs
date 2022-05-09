using Content.Shared.ShrinkRay;
using Robust.Client.GameObjects;

namespace Content.Client.ShrinkRay
{
    public sealed class ShrinkRaySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShrunkenSpriteComponent, ComponentShutdown>(OnShutdown);
            SubscribeNetworkEvent<SizeChangedEvent>(ApplySize);
        }

        private void ApplySize(SizeChangedEvent args)
        {
            if (!TryComp<SpriteComponent>(args.Target, out var sprite))
                return;
            ShrunkenSpriteComponent shrunken = new();
            shrunken.Owner = args.Target;
            shrunken.OriginalScaleFactor = sprite.Scale;
            shrunken.ScaleFactor = args.Scale;
            EntityManager.AddComponent<ShrunkenSpriteComponent>(args.Target, shrunken, true); // So we can cleanly overwrite

            sprite.Scale = shrunken.ScaleFactor;
        }
        private void OnShutdown(EntityUid uid, ShrunkenSpriteComponent component, ComponentShutdown args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            sprite.Scale = component.OriginalScaleFactor;
        }
    }
}
