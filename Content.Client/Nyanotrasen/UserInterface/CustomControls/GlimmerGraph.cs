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
            var box = new UIBox2((15, 230), (465, 30));
            handle.DrawRect(box, Color.FromHex("#424245"));
            var texture = _resourceCache.GetTexture("/Textures/Interface/glimmerGraph.png");
            handle.DrawTexture(texture, (15, 30));

            if (_glimmer.Count < 2)
                return;

            var spacing = 450 / (_glimmer.Count - 1);

            var i = 0;
            while (i + 1 < _glimmer.Count)
            {
                Vector2 vector1 = (15 + i * spacing, 230 - _glimmer[i] / 5);
                Vector2 vector2 = (15 + (i + 1) * spacing, 230 - _glimmer[i + 1] / 5);
                handle.DrawLine(vector1, vector2, Color.FromHex("#A200BB"));
                handle.DrawLine(vector1 + (0, 1), vector2 + (0, 1), Color.FromHex("#A200BB"));
                handle.DrawLine(vector1 - (0, 1), vector2 - (0, 1), Color.FromHex("#A200BB"));
                handle.DrawLine((15 + i * spacing, 230), (15 + i * spacing, 30), Color.FromHex("#686868"));
                i++;
            }
        }
    }
}
