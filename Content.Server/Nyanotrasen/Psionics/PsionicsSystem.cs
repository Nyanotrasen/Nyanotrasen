using Content.Shared.Abilities.Psionics;
using Content.Server.Weapon.Melee;
using Content.Server.Stunnable;
using Content.Server.Damage.Events;
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
            SubscribeLocalEvent<PotentialPsionicComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<AntiPsionicWeaponComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<AntiPsionicWeaponComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnInit(EntityUid uid, PotentialPsionicComponent component, ComponentInit args)
        {
            if (_random.Prob(component.Chance))
                _psionicAbilitiesSystem.AddPsionics(uid);
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
