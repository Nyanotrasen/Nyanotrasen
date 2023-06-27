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
            var texture = _resourceCache.GetTexture("/Textures/Interface/glimmerGraph.png");
            handle.DrawTexture(texture, (0, 50));

            if (_glimmer.Count < 2)
                return;

            var spacing = 450 / (_glimmer.Count - 1);

            var i = 0;
            while (i + 1 < _glimmer.Count)
            {
                Vector2 vector1 = (i * spacing, 250 - _glimmer[i] / 5);
                Vector2 vector2 = ((i + 1) * spacing, 250 - _glimmer[i + 1] / 5);
                handle.DrawLine(vector1, vector2, Color.FromHex("#A200BB"));
                handle.DrawLine(vector1 + (0, 1), vector2 + (0, 1), Color.FromHex("#A200BB"));
                handle.DrawLine(vector1 - (0, 1), vector2 - (0, 1), Color.FromHex("#A200BB"));
                handle.DrawLine((i * spacing, 250), (i * spacing, 50), Color.FromHex("#686868"));
                i++;
            }
        }
    }
}
