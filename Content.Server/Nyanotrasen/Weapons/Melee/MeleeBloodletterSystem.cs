using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Wieldable.Components;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Weapons.Melee
{
    public sealed class MeleeBloodletterSystem : EntitySystem
    {
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MeleeBloodletterComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, MeleeBloodletterComponent component, MeleeHitEvent args)
        {
            if (!args.IsHit)
                return;

            foreach (var hit in args.HitEntities)
            {
                if (!TryComp(hit, out BloodstreamComponent? bloodstream))
                    continue;

                float coefficient = 1.0f;

                if (TryComp(uid, out WieldableComponent? wieldableComponent) && wieldableComponent.Wielded)
                    coefficient = component.WieldCoefficient;

                if (component.BloodReduction != null)
                {
                    var ev = new DamageModifyEvent(component.BloodReduction * coefficient);
                    RaiseLocalEvent(hit, ev, false);

                    if (!ev.Damage.Empty)
                        _bloodstreamSystem.TryModifyBloodLevel(hit, -ev.Damage.Total, bloodstream);
                }

                if (component.BleedingIncrease != null)
                {
                    var ev = new DamageModifyEvent(component.BleedingIncrease * coefficient);
                    RaiseLocalEvent(hit, ev, false);

                    if (!ev.Damage.Empty)
                        _bloodstreamSystem.TryModifyBleedAmount(hit, (float) ev.Damage.Total, bloodstream);
                }

            }
        }
    }
}
