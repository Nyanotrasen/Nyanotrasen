using Content.Shared.Damage;
using Content.Shared.Inventory.Events;
using Content.Server.Abilities.Gachi.Components;
using Content.Server.Clothing.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Abilities.Gachi
{
    public sealed class GachiSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GachiComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<JabroniOutfitComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<JabroniOutfitComponent, GotUnequippedEvent>(OnUnequipped);
        }

        private void OnDamageChanged(EntityUid uid, GachiComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased && args.DamageDelta != null && args.DamageDelta.Total >= 5)
                SoundSystem.Play(Filter.Pvs(uid), "/Audio/Effects/Gachi/van_fuckyou.ogg", uid);
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
