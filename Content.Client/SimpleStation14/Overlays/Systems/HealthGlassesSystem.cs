using Content.Client.HealthOverlay;
using Content.Shared.Inventory.Events;
using Content.Shared.SimpleStation14.Clothing;

namespace Content.Client.SimpleStation14.Overlays
{
    public sealed class HealthGlassesSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HealthGlassesComponent, ComponentRemove>(OnRemove);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var glassed in EntityQuery<HealthGlassesComponent>())
            {
                var system = EntitySystem.Get<HealthOverlaySystem>();
                if (!system.Enabled) system.Enabled = true;
            }
        }

        private void OnRemove(EntityUid uid, HealthGlassesComponent component, ComponentRemove args)
        {
            var system = EntitySystem.Get<HealthOverlaySystem>();
            system.Enabled = false;
        }
    }
}
