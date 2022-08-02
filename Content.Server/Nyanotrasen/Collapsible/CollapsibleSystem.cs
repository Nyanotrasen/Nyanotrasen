using Content.Shared.Interaction.Events;
using Content.Shared.Collapsible;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared.Item;

namespace Content.Server.Collapsible
{
    public sealed class CollapsibleSystem : EntitySystem
    {
        [Dependency] private readonly SharedItemSystem _itemSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollapsibleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<CollapsibleComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnInit(EntityUid uid, CollapsibleComponent component, ComponentInit args)
        {
            UpdateCollapsibleVisuals(uid, component.Collapsed);
        }
        private void OnUseInHand(EntityUid uid, CollapsibleComponent component, UseInHandEvent args)
        {
            ToggleCollapse(uid, component);
        }

        public void ToggleCollapse(EntityUid uid, CollapsibleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Collapsed = !component.Collapsed;
            if (!component.Collapsed && component.ExtendSound != null)
                SoundSystem.Play(component.ExtendSound.GetSound(), Filter.Pvs(uid), uid);

            if (TryComp<ItemComponent>(uid, out var item))
            {
                if (!component.Collapsed)
                {
                    _itemSystem.SetSize(uid, item.Size * 15, item);
                } else
                {
                    _itemSystem.SetSize(uid, item.Size / 15, item);
                }
            }

            UpdateCollapsibleVisuals(uid, component.Collapsed);
        }

        private void UpdateCollapsibleVisuals(EntityUid uid, bool isCollapsed)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(CollapsibleVisuals.IsCollapsed, isCollapsed);
            appearance.SetData(CollapsibleVisuals.InhandsVisible, !isCollapsed);
        }
    }
}
