using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.IgnoreHumanoids;

/// <summary>
/// Stops drones from telling people apart.
/// </summary>
public sealed class IgnoreHumanoidsOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transform;
    private readonly Texture _barTexture;
    private readonly ShaderInstance _shader;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public IgnoreHumanoidsOverlay(IEntityManager entManager, IPrototypeManager protoManager)
    {
        _entManager = entManager;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

        var sprite = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/health_bar.rsi"), "icon");
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

        foreach (var humanoid in _entManager.EntityQuery<HumanoidAppearanceComponent>(true))
        {
            if (!spriteQuery.TryGetComponent(humanoid.Owner, out var sprite))
            {
                continue;
            }

            sprite.Visible = false;
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }
}
