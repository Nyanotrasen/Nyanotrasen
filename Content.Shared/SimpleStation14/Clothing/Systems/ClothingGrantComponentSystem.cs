using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Tag;

namespace Content.Shared.SimpleStation14.Clothing;

public sealed class ClothingGrantComponentSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingGrantComponentComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<ClothingGrantComponentComponent, GotUnequippedEvent>(OnUnequip);
    }

    private void OnEquip(EntityUid uid, ClothingGrantComponentComponent component, GotEquippedEvent args)
    {
        // This only works on clothing
        if (!TryComp<ClothingComponent>(uid, out var clothing)) return;

        // Is the clothing in its actual slot?
        if (!clothing.Slots.HasFlag(args.SlotFlags)) return;

        if (component.Component != null)
        {
            // does the user already has this component?
            var componentType = _componentFactory.GetRegistration(component.Component).Type;
            if (EntityManager.HasComponent(args.Equipee, componentType)) return;

            var newComponent = (Component) _componentFactory.GetComponent(componentType);
            newComponent.Owner = args.Equipee;

            EntityManager.AddComponent(args.Equipee, newComponent);

            component.IsActive = true;
        }
        if (component.Tag != null)
        {
            EnsureComp<TagComponent>(args.Equipee);
            _tagSystem.AddTag(args.Equipee, component.Tag);

            component.IsActive = true;
        }
    }

    private void OnUnequip(EntityUid uid, ClothingGrantComponentComponent component, GotUnequippedEvent args)
    {
        if (!component.IsActive) return;

        if (component.Component != null)
        {
            component.IsActive = false;

            var componentType = _componentFactory.GetRegistration(component.Component).Type;
            if (EntityManager.HasComponent(args.Equipee, componentType))
            {
                EntityManager.RemoveComponent(args.Equipee, componentType);
            }
        }
        if (component.Tag != null)
        {
            component.IsActive = false;

            EnsureComp<TagComponent>(args.Equipee);
            _tagSystem.RemoveTag(args.Equipee, component.Tag);
        }
    }
}
