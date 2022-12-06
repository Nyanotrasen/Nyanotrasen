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

            if (TryComp(uid, out WieldableComponent? wieldableComponent) && wieldableComponent.Wielded)
                coefficient = component.WieldCoefficient;

            _staminaSystem.TakeStaminaDamage(
                args.User,
                // We can see if it's a swing or a hit by checking if there are any HitEntities.
                coefficient * (component.SwingCost + (args.HitEntities.Any() ? component.HitCost : 0)),
                staminaComponent);
        }
    }
}
