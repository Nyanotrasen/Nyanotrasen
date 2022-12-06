using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Weapons.Melee
{
    public sealed class DamageWeaponOnHitSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DamageWeaponOnHitComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, DamageWeaponOnHitComponent component, MeleeHitEvent args)
        {
            if (!args.IsHit ||
                !args.HitEntities.Any())
                return;

            _damageableSystem.TryChangeDamage(uid, component.Damage);
        }
    }
}
