using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Nyanotrasen.Overlays
{
    public sealed class DogVisionOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _dogVisionShader;

        public DogVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
            _dogVisionShader = _prototypeManager.Index<ShaderPrototype>("DogVision").Instance().Duplicate();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            _dogVisionShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);


            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.UseShader(_dogVisionShader);
            worldHandle.DrawRect(viewport, Color.White);
        }
    }
}
