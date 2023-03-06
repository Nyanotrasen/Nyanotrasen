using System.Linq;
using Content.Server.Wieldable.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Weapons.Melee
{
    public sealed class MeleeStaminaCostSystem : EntitySystem
    {
        [Dependency] private readonly StaminaSystem _staminaSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MeleeStaminaCostComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, MeleeStaminaCostComponent component, MeleeHitEvent args)
        {
            // MeleeHitEvent is raised when examining a weapon with IsHit set to false.
            // Not the best naming scheme.
            if (!args.IsHit)
                return;

            if (!TryComp(args.User, out StaminaComponent? staminaComponent))
                return;

            float coefficient = 1.0f;

            if (args.HeavyAttack == true)
                coefficient = component.HeavyStaminaCostModifier;

            // We can see if it's a swing or a hit by checking if there are any HitEntities.
            float staminaDamage = coefficient * (component.SwingCost + (args.HitEntities.Any() ? component.HitCost : 0f));
            if (TryComp(uid, out WieldableComponent? wieldableComponent) && wieldableComponent.Wielded)
                staminaDamage -= component.WieldModifier;

            float meleeCostLimit = staminaComponent.CritThreshold * staminaComponent.MeleeCostLimitFactor;

            // Avoid GetStaminaDamage: it uses some time-based prediction calculation.
            if (staminaComponent.StaminaDamage + staminaDamage > meleeCostLimit)
                staminaDamage = meleeCostLimit - staminaComponent.StaminaDamage;

            const float ImpermissibleLimit = 0.01f;
            if (staminaDamage <= ImpermissibleLimit)
                return;

            _staminaSystem.TakeStaminaDamage(
                args.User,
                staminaDamage,
                staminaComponent);
        }
    }
}
