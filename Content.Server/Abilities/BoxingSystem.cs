using Content.Server.Weapon.Melee;
using Content.Shared.StatusEffect;
using Content.Shared.Sound;
using Content.Server.Stunnable;
using Content.Shared.Stunnable;
using Content.Shared.Inventory.Events;
using Content.Server.Weapon.Melee.Components;
using Content.Server.Clothing.Components;
using Robust.Shared.Random;

namespace Content.Server.Abilities.Boxer
{
    public sealed class BoxingSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BoxerComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<BoxingGlovesComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<BoxingGlovesComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnMeleeHit(EntityUid uid, BoxerComponent component, MeleeHitEvent args)
        {
            if (!component.Enabled)
                return;

            foreach (var entity in args.HitEntities)
            {
                if (!TryComp<StatusEffectsComponent>(entity, out var status))
                    continue;

                if (!HasComp<SlowedDownComponent>(entity))
                {
                    if (_robustRandom.Prob(component.ParalyzeChanceNoSlowdown))
                        _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(component.ParalyzeTime), true, status);
                    else
                        _stunSystem.TrySlowdown(entity, TimeSpan.FromSeconds(component.SlowdownTime), true,  0.5f, 0.5f, status);
                }
                else
                {
                    if (_robustRandom.Prob(component.ParalyzeChanceWithSlowdown))
                        _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(component.ParalyzeTime), true, status);
                    else
                        _stunSystem.TrySlowdown(entity, TimeSpan.FromSeconds(component.SlowdownTime), true,  0.5f, 0.5f, status);
                }
            }
        }
        private void OnEquipped(EntityUid uid, BoxingGlovesComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.SlotFlags.HasFlag(args.SlotFlags))
                return;
            if (TryComp<BoxerComponent>(args.Equipee, out var boxer))
                boxer.Enabled = true;

            // Set the component to active to the unequip check isn't CBT
            component.IsActive = true;

            if (TryComp<MeleeWeaponComponent>(args.Equipee, out var meleeComponent))
                meleeComponent.HitSound = component.HitSound;
        }

        private void OnUnequipped(EntityUid uid, BoxingGlovesComponent component, GotUnequippedEvent args)
        {
            // Only undo the resistance if it was affecting the user
            if (!component.IsActive)
                return;
            if(TryComp<BoxerComponent>(args.Equipee, out var boxer))
                boxer.Enabled = false;
            if (TryComp<MeleeWeaponComponent>(args.Equipee, out var meleeComponent))
                meleeComponent.HitSound = new SoundCollectionSpecifier("GenericHit");
            component.IsActive = false;
        }

    }
}
