using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Server.Abilities.Gachi.Components;
using Content.Server.Clothing.Components;
using Content.Server.Weapon.Melee;
using Content.Shared.MobState;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Abilities.Gachi
{
    public sealed class GachiSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GachiComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<GachiComponent, MeleeHitEvent>(OnMeleeHit);
            SubscribeLocalEvent<GachiComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<JabroniOutfitComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<JabroniOutfitComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnDamageChanged(EntityUid uid, GachiComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased && args.DamageDelta != null && args.DamageDelta.Total >= 5 && _random.Prob(0.3f))
            {
                if (_random.Prob(0.01f))
                {
                    SoundSystem.Play(Filter.Pvs(uid), "/Audio/Effects/Gachi/ripears.ogg", uid, AudioParams.Default.WithVolume(8f));
                    return;
                }
                SoundSystem.Play(Filter.Pvs(uid), component.PainSound.GetSound(), uid);
            }
        }

        private void OnMeleeHit(EntityUid uid, GachiComponent component, MeleeHitEvent args)
        {
            if (_random.Prob(0.2f))
            {
                SoundSystem.Play(Filter.Pvs(uid), component.HitOtherSound.GetSound(), uid);
            }
        }

        private void OnMobStateChanged(EntityUid uid, GachiComponent component, MobStateChangedEvent args)
        {
            if (args.CurrentMobState.IsCritical())
            {
                SoundSystem.Play(Filter.Pvs(uid), "/Audio/Effects/Gachi/knockedhimout.ogg", uid);
            }
        }

        private void OnEquipped(EntityUid uid, JabroniOutfitComponent component, GotEquippedEvent args)
        {
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            if (!clothing.SlotFlags.HasFlag(args.SlotFlags))
                return;
            EnsureComp<GachiComponent>(args.Equipee);
            component.IsActive = true;
        }

        private void OnUnequipped(EntityUid uid, JabroniOutfitComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;
            component.IsActive = false;
            RemComp<GachiComponent>(uid);
        }
    }
}
