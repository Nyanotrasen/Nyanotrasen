using Content.Shared.Damage;
using Content.Server.Abilities.Gachi.Components;
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
        }

        private void OnDamageChanged(EntityUid uid, GachiComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased && args.DamageDelta != null && args.DamageDelta.Total >= 5)
                SoundSystem.Play(Filter.Pvs(uid), "/Audio/Effects/Gachi/van_fuckyou.ogg", uid);
        }
    }
}
