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

            int spacing = 450 / (_glimmer.Count - 1);

            int i = 0;
            while (i + 1 < _glimmer.Count)
            {
                handle.DrawLine((i * spacing, 250 - _glimmer[i] / 5), ((i + 1) * spacing, 250 - _glimmer[i + 1] / 5), Color.Red);
                handle.DrawLine((i * spacing, 250), (i * spacing, 50), Color.White);
                i++;
            }
        }
    }
}
