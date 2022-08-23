using Content.Shared.Abilities.Psionics;
using Content.Server.Abilities.Psionics;
using Content.Server.Weapon.Melee;
using Content.Server.Stunnable;
using Content.Server.Damage.Events;
using Content.Server.GameTicking;
using Robust.Shared.Random;

namespace Content.Server.Psionics
{
    public sealed class PsionicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PotentialPsionicComponent, PlayerSpawnCompleteEvent>(OnStartup);
            SubscribeLocalEvent<GuaranteedPsionicComponent, PlayerSpawnCompleteEvent>(OnGuaranteedStartup);
            SubscribeLocalEvent<AntiPsionicWeaponComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<AntiPsionicWeaponComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnStartup(EntityUid uid, PotentialPsionicComponent component, PlayerSpawnCompleteEvent args)
        {
            if (HasComp<GuaranteedPsionicComponent>(uid))
                return;

            var chance = component.Chance;

            if (TryComp<PsionicBonusChanceComponent>(uid, out var bonus))
            {
                chance *= bonus.Multiplier;
                chance += bonus.FlatBonus;
            }

            chance = Math.Clamp(chance, 0, 1);
            if (_random.Prob(chance))
                _psionicAbilitiesSystem.AddPsionics(uid);
        }

        private void OnGuaranteedStartup(EntityUid uid, GuaranteedPsionicComponent component, PlayerSpawnCompleteEvent args)
        {
            if (component.PowerComponent == null)
            {
                _psionicAbilitiesSystem.AddPsionics(uid);
                return;
            }

            _psionicAbilitiesSystem.AddPsionics(uid, component.PowerComponent);
        }

        private void OnMeleeHit(EntityUid uid, AntiPsionicWeaponComponent component, MeleeHitEvent args)
        {
            foreach (var entity in args.HitEntities)
            {
                if (HasComp<PsionicComponent>(entity))
                {
                    args.ModifiersList.Add(component.Modifiers);
                    if (_random.Prob(component.StunChance))
                        _stunSystem.TryParalyze(entity, TimeSpan.FromSeconds(2f), false);
                }
            }
        }

        private void OnStamHit(EntityUid uid, AntiPsionicWeaponComponent component, StaminaMeleeHitEvent args)
        {
            args.Multiplier *= component.PsychicDamageMultiplier;
        }
    }
}
