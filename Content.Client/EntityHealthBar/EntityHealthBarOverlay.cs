using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.EntityHealthBar;

public sealed class EntityHealthBarOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transform;
    private readonly Texture _barTexture;
    private readonly ShaderInstance _shader;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public string? DamageContainer;

    public EntityHealthBarOverlay(IEntityManager entManager, IPrototypeManager protoManager)
    {
        _entManager = entManager;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var sprite = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/progress_bar.rsi"), "icon");
        _barTexture = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);

        _shader = protoManager.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        const float scale = 1f;
        var scaleMatrix = Matrix3.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3.CreateRotation(-rotation);
        handle.UseShader(_shader);

        foreach (var (mob, dmg) in _entManager.EntityQuery<MobStateComponent, DamageableComponent>(true))
        {
            if (!xformQuery.TryGetComponent(mob.Owner, out var xform) ||
                xform.MapID != args.MapId)
            {
                continue;
            }

            var worldPosition = _transform.GetWorldPosition(xform);
            var worldMatrix = Matrix3.CreateTranslation(worldPosition);

            Matrix3.Multiply(scaleMatrix, worldMatrix, out var scaledWorld);
            Matrix3.Multiply(rotationMatrix, scaledWorld, out var matty);

            handle.SetTransform(matty);
            // Use the sprite itself if we know its bounds. This means short or tall sprites don't get overlapped
            // by the bar.
            float yOffset;
            if (spriteQuery.TryGetComponent(mob.Owner, out var sprite))
            {
                yOffset = sprite.Bounds.Height / 2f + 0.05f;
            }
            else
            {
                yOffset = 0.5f;
            }

            // Position above the entity (we've already applied the matrix transform to the entity itself)
            // Offset by the texture size for every do_after we have.
            var position = new Vector2(-_barTexture.Width / 2f / EyeManager.PixelsPerMeter,
                yOffset / scale / EyeManager.PixelsPerMeter * scale);

            // Draw the underlying bar texture
            handle.DrawTexture(_barTexture, position);

            // Hardcoded width of the progress bar because it doesn't match the texture.
            const float startX = 2f;
            const float endX = 22f;

            var xProgress = (endX - startX) * 1 + startX;

            var box = new Box2(new Vector2(startX, 3f) / EyeManager.PixelsPerMeter, new Vector2(xProgress, 4f) / EyeManager.PixelsPerMeter);
            box = box.Translated(position);
            handle.DrawRect(box, Color.Green);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }

    public static Color GetProgressColor(float progress)
    {
        if (progress >= 1.0f)
        {
            return new Color(0f, 1f, 0f);
        }
        // lerp
        var hue = (5f / 18f) * progress;
        return Color.FromHsv((hue, 1f, 0.75f, 1f));
    }
}
