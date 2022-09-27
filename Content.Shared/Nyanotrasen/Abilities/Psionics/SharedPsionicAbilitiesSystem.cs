using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;


namespace Content.Shared.Abilities.Psionics
{
    public sealed class SharedPsionicAbilitiesSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedPopupSystem _popups = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TinfoilHatComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<TinfoilHatComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<ClothingGrantPsionicPowerComponent, GotEquippedEvent>(OnGranterEquipped);
            SubscribeLocalEvent<ClothingGrantPsionicPowerComponent, GotUnequippedEvent>(OnGranterUnequipped);
            SubscribeLocalEvent<PsionicsDisabledComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PsionicsDisabledComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PsionicComponent, PsionicPowerUsedEvent>(OnPowerUsed);
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
            var insul = EnsureComp<PsionicInsulationComponent>(args.Equipee);
            insul.Passthrough = component.Passthrough;
            component.IsActive = true;
        }

        private void OnUnequipped(EntityUid uid, TinfoilHatComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            if (!_statusEffects.HasStatusEffect(uid, "PsionicallyInsulated"))
                RemComp<PsionicInsulationComponent>(args.Equipee);

            component.IsActive = false;

            if (!HasComp<PsionicsDisabledComponent>(args.Equipee))
                TogglePsionics(args.Equipee, true);
        }

        private void OnGranterEquipped(EntityUid uid, ClothingGrantPsionicPowerComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<SharedClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;
            // does the user already has this power?
            var componentType = _componentFactory.GetRegistration(component.Power).Type;
            if (EntityManager.HasComponent(args.Equipee, componentType)) return;


            var newComponent = (Component) _componentFactory.GetComponent(componentType);
            newComponent.Owner = args.Equipee;

            EntityManager.AddComponent(args.Equipee, newComponent);

            component.IsActive = true;
        }

        private void OnGranterUnequipped(EntityUid uid, ClothingGrantPsionicPowerComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            component.IsActive = false;
            var componentType = _componentFactory.GetRegistration(component.Power).Type;
            if (EntityManager.HasComponent(args.Equipee, componentType))
            {
                EntityManager.RemoveComponent(args.Equipee, componentType);
            }
        }

        private void OnPowerUsed(EntityUid uid, PsionicComponent component, PsionicPowerUsedEvent args)
        {
            foreach (var entity in _lookup.GetEntitiesInRange(uid, 10f))
            {
                if (HasComp<MetapsionicPowerComponent>(entity) && entity != uid && !(TryComp<PsionicInsulationComponent>(entity, out var insul) && !insul.Passthrough))
                {
                    _popups.PopupEntity(Loc.GetString("metapsionic-pulse-power", ("power", args.Power)), entity, Filter.Entities(entity), PopupType.LargeCaution);
                    args.Handled = true;
                    return;
                }
            }
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
        public void LogPowerUsed(EntityUid uid, string power, int minGlimmer = 8, int maxGlimmer = 12)
        {
            _adminLogger.Add(Database.LogType.Psionics, Database.LogImpact.Medium, $"{ToPrettyString(uid):player} used {power}");
            var ev = new PsionicPowerUsedEvent(uid, power);
            RaiseLocalEvent(uid, ev, false);

            _glimmerSystem.AddToGlimmer(_robustRandom.Next(minGlimmer, maxGlimmer));
        }
    }
    
    public sealed class PsionicPowerUsedEvent : HandledEntityEventArgs 
    {
        public EntityUid User { get; }
        public string Power = string.Empty;

        public PsionicPowerUsedEvent(EntityUid user, string power)
        {
            User = user;
            Power = power;
        }
    }

    [Serializable]
    [NetSerializable]
    public sealed class PsionicsChangedEvent : EntityEventArgs
    {
        public readonly EntityUid Euid;
        public PsionicsChangedEvent(EntityUid euid)
        {
            Euid = euid;
        }
    }
}