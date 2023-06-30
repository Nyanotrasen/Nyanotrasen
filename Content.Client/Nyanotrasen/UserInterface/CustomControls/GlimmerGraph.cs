using Robust.Client.UserInterface;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Content.Client.Resources;

namespace Content.Client.Nyanotrasen.UserInterface.CustomControls
{
    public sealed class GlimmerGraph : Control
    {
        private readonly IResourceCache _resourceCache;
        private readonly List<int> _glimmer;
        private const int XOffset = 15;
        private const int YOffset = 210;
        private const int Length = 450;
        private static int YOffsetTop => YOffset - 200;
        public GlimmerGraph(IResourceCache resourceCache, List<int> glimmer)
        {
            _resourceCache = resourceCache;
            _glimmer = glimmer;
            HorizontalAlignment = HAlignment.Left;
            VerticalAlignment = VAlignment.Bottom;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);
            var box = new UIBox2((XOffset, YOffset), (XOffset + Length, YOffsetTop));
            handle.DrawRect(box, Color.FromHex("#424245"));
            var texture = _resourceCache.GetTexture("/Textures/Interface/glimmerGraph.png");
            handle.DrawTexture(texture, (XOffset, YOffsetTop));

            if (_glimmer.Count < 2)
                return;

            var spacing = Length / (_glimmer.Count - 1);

            var i = 0;
            while (i + 1 < _glimmer.Count)
            {
                Vector2 vector1 = (XOffset + i * spacing, YOffset - _glimmer[i] / 5);
                Vector2 vector2 = (XOffset + (i + 1) * spacing, YOffset - _glimmer[i + 1] / 5);
                handle.DrawLine(vector1, vector2, Color.FromHex("#A200BB"));
                handle.DrawLine(vector1 + (0, 1), vector2 + (0, 1), Color.FromHex("#A200BB"));
                handle.DrawLine(vector1 - (0, 1), vector2 - (0, 1), Color.FromHex("#A200BB"));
                handle.DrawLine((XOffset + i * spacing, YOffset), (XOffset + i * spacing, YOffsetTop), Color.FromHex("#686868"));
                i++;
            }
        }
    }
}
