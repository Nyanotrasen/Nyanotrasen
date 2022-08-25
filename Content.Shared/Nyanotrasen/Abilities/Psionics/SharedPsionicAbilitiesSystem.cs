using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Actions;

namespace Content.Shared.Abilities.Psionics
{
    public sealed class SharedPsionicAbilitiesSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TinfoilHatComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<TinfoilHatComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<PsionicsDisabledComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PsionicsDisabledComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnEquipped(EntityUid uid, TinfoilHatComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<SharedClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;
            
            TogglePsionics(args.Equipee, false);
            EnsureComp<PsionicInsulationComponent>(args.Equipee);
            component.IsActive = true;
        }

        private void OnUnequipped(EntityUid uid, TinfoilHatComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            RemComp<PsionicInsulationComponent>(args.Equipee);
            component.IsActive = false;

            if (!HasComp<PsionicsDisabledComponent>(args.Equipee))
                TogglePsionics(args.Equipee, true);
        }


        private void OnInit(EntityUid uid, PsionicsDisabledComponent component, ComponentInit args)
        {
            TogglePsionics(uid, false);
        }

        private void OnShutdown(EntityUid uid, PsionicsDisabledComponent component, ComponentShutdown args)
        {
            if (!HasComp<PsionicInsulationComponent>(uid))
                TogglePsionics(uid, true);
        }
        public void TogglePsionics(EntityUid uid, bool toggle, PsionicComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            if (component.PsionicAbility == null)
                return;

            _actions.SetEnabled(component.PsionicAbility, toggle);
        }
    }
}