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

            var shrunken = EnsureComp<ShrunkenSpriteComponent>(args.Target);
            shrunken.OriginalScaleFactor = sprite.Scale;
            shrunken.ScaleFactor = args.Scale;
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
