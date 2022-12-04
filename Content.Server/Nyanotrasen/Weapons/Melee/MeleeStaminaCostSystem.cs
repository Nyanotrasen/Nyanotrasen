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
            if (!TryComp(args.User, out StaminaComponent? staminaComponent))
                return;

            _staminaSystem.TakeStaminaDamage(
                args.User,
                component.SwingCost + (args.IsHit ? component.HitCost : 0),
                staminaComponent);
        }
    }
}
