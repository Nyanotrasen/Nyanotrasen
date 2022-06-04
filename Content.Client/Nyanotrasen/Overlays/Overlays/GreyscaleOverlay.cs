using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Nyanotrasen.Overlays
{
    public sealed class GreyscaleOverlay : Overlay
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _greyscaleShader;

        public GreyscaleOverlay()
        {
            IoCManager.InjectDependencies(this);
            _greyscaleShader = _prototypeManager.Index<ShaderPrototype>("GreyscaleS").Instance().Duplicate();
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            _greyscaleShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);


            var worldHandle = args.WorldHandle;
            var viewport = args.WorldAABB;
            worldHandle.SetTransform(Matrix3.Identity);
            worldHandle.UseShader(_greyscaleShader);
            worldHandle.DrawRect(args.WorldAABB, Color.White);
        }
    }
}
