using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.EntityHealthBar
{
    [UsedImplicitly]
    public sealed class EntityHealthBarOverlay : Overlay
    {
        private readonly IEntityManager _entMan = default!;
        private readonly SharedTransformSystem _transformSys = default!;
        private readonly Texture _barTexture;
        private readonly ShaderInstance _shader;
        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
        public EntityHealthBarOverlay(IEntityManager entMan, IPrototypeManager protoManager)
        {
            _entMan = entMan;
            _transformSys = _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

            // did I duplicate doafters, you ask?
            // well maybe but health_bar.rsi was actually an exact copy of progress_bar.rsi lol
            var sprite = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/progress_bar.rsi"), "icon");
            _barTexture = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);

            _shader = protoManager.Index<ShaderPrototype>("unshaded").Instance();
        }

        /// <summary>
        /// Occlude other bars based on line-of-sight
        /// </summary>
        public bool CheckLOS = false;
        /// <summary>
        /// If this is not null, we'll only use this damage container.
        /// </summary>
        public string? DamageContainer;
        protected override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.WorldHandle;
            var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
            var spriteQuery = _entMan.GetEntityQuery<SpriteComponent>();
            var xformQuery = _entMan.GetEntityQuery<TransformComponent>();

            // If you use the display UI scale then need to set max(1f, displayscale) because 0 is valid.
            const float scale = 1f;
            var scaleMatrix = Matrix3.CreateScale(new Vector2(scale, scale));
            var rotationMatrix = Matrix3.CreateRotation(-rotation);
            handle.UseShader(_shader);

            foreach (var (mobState, damageable, theirxform) in _entMan.EntityQuery<MobStateComponent, DamageableComponent, TransformComponent>())
            {
                if (!xformQuery.TryGetComponent(mobState.Owner, out var xform) ||
                    xform.MapID != args.MapId)
                {
                    continue;
                }

                var worldPosition = _transformSys.GetWorldPosition(xform);
                var worldMatrix = Matrix3.CreateTranslation(worldPosition);

                if (DamageContainer != null && damageable.DamageContainerID != DamageContainer)
                    continue;

                Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
                Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);

                float yOffset;
                if (spriteQuery.TryGetComponent(mobState.Owner, out var sprite))
                {
                    yOffset = sprite.Bounds.Height / 2f + 0.05f;
                }
                else
                {
                    yOffset = 0.5f;
                }

                var position = new Vector2(-_barTexture.Width / 2f / EyeManager.PixelsPerMeter,
                    yOffset / scale / EyeManager.PixelsPerMeter * scale);

                handle.DrawTexture(_barTexture, position);

                const float startX = 2f;
                const float endX = 22f;
                var xProgress = (endX - startX) * 1 + startX;

                var box = new Box2(new Vector2(startX, 3f) / EyeManager.PixelsPerMeter, new Vector2(xProgress, 4f) / EyeManager.PixelsPerMeter);
                box = box.Translated(position);
                handle.DrawRect(box, Color.Red);
            }
            handle.UseShader(null);
            handle.SetTransform(Matrix3.Identity);
        }
    }
}
