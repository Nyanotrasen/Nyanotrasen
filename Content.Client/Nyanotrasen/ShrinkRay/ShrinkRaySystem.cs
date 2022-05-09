using Content.Shared.ShrinkRay;
using Robust.Client.GameObjects;

namespace Content.Client.ShrinkRay
{
    public sealed class ShrinkRaySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShrunkenSpriteComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShrunkenSpriteComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnInit(EntityUid uid, ShrunkenSpriteComponent component, ComponentInit args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            component.OriginalScaleFactor = sprite.Scale;
            sprite.Scale = component.ScaleFactor;
        }


        private void OnShutdown(EntityUid uid, ShrunkenSpriteComponent component, ComponentShutdown args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            sprite.Scale = component.OriginalScaleFactor;
        }
    }
}
