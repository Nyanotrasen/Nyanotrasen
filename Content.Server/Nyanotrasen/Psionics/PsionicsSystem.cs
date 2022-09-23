using Content.Shared.Abilities.Psionics;
using Content.Shared.StatusEffect;
using Content.Server.Abilities.Psionics;
using Content.Server.Weapon.Melee;
using Content.Server.Damage.Events;
using Content.Server.GameTicking;
using Content.Server.Electrocution;
using Robust.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Psionics
{
    public sealed class PsionicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
        [Dependency] private readonly MindSwapPowerSystem _mindSwapPowerSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PotentialPsionicComponent, PlayerSpawnCompleteEvent>(OnStartup);
            SubscribeLocalEvent<GuaranteedPsionicComponent, PlayerSpawnCompleteEvent>(OnGuaranteedStartup);
            SubscribeLocalEvent<AntiPsionicWeaponComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<AntiPsionicWeaponComponent, StaminaMeleeHitEvent>(OnStamHit);

            SubscribeLocalEvent<PsionicComponent, ComponentInit>(OnPsiInit);
            SubscribeLocalEvent<PsionicComponent, ComponentShutdown>(OnPsiShutdown);
        }

        private void OnStartup(EntityUid uid, PotentialPsionicComponent component, PlayerSpawnCompleteEvent args)
        {
            RollPsionics(uid, component);
        }

        private void OnGuaranteedStartup(EntityUid uid, GuaranteedPsionicComponent component, PlayerSpawnCompleteEvent args)
        {
            if (component.PowerComponent == null)
            {
                _psionicAbilitiesSystem.AddRandomPsionicPower(uid);
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
                    SoundSystem.Play("/Audio/Effects/lightburn.ogg", Filter.Pvs(entity), entity);
                    args.ModifiersList.Add(component.Modifiers);
                    if (_random.Prob(component.DisableChance))
                        _statusEffects.TryAddStatusEffect(entity, "PsionicsDisabled", TimeSpan.FromSeconds(10), true, "PsionicsDisabled");
                }

                if (TryComp<MindSwappedComponent>(entity, out var swapped))
                {
                    _mindSwapPowerSystem.Swap(entity, swapped.OriginalEntity, true);
                    return;
                }

                if (HasComp<PotentialPsionicComponent>(entity) && !HasComp<PsionicComponent>(entity) && _random.Prob(0.5f))
                    _electrocutionSystem.TryDoElectrocution(args.User, null, 20, TimeSpan.FromSeconds(5), false);
            }
        }

        private void OnPsiInit(EntityUid uid, PsionicComponent component, ComponentInit args)
        {
            InformPsionicsChanged(uid);
        }

        private void OnPsiShutdown(EntityUid uid, PsionicComponent component, ComponentShutdown args)
        {
            InformPsionicsChanged(uid);
        }

        private void InformPsionicsChanged(EntityUid uid)
        {
            RaiseNetworkEvent(new PsionicsChangedEvent(uid), Filter.Entities(uid));
        }

        private void OnStamHit(EntityUid uid, AntiPsionicWeaponComponent component, StaminaMeleeHitEvent args)
        {
            args.FlatModifier += component.PsychicStaminaDamage;
        }

        public void RollPsionics(EntityUid uid, PotentialPsionicComponent component)
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

        public void RerollPsionics(EntityUid uid, PotentialPsionicComponent? psionic = null)
        {
            if (!Resolve(uid, ref psionic, false))
                return;

            if (psionic.Rerolled)
                return;

            RollPsionics(uid, psionic);
            psionic.Rerolled = true;
        }
    }
}
