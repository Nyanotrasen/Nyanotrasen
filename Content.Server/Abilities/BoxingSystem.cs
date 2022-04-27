using Content.Server.Weapon.Melee;
using Content.Shared.StatusEffect;
using Content.Server.Stunnable;
using Content.Shared.Stunnable;
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
    }
}
