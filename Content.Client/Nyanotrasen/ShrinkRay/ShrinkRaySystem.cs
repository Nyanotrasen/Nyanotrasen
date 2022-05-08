using Content.Shared.ShrinkRay;
using Robust.Client.GameObjects;

namespace Content.Client.ShrinkRay
{
    public sealed class ShrinkRaySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedShrunkenComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, SharedShrunkenComponent component, ComponentInit args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            component.OriginalScaleFactor = sprite.Scale;
            sprite.Scale = component.ScaleFactor;
        }

    }
}
