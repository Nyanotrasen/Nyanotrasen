using Content.Shared.Actions;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Abilities.Felinid
{
    public sealed class FelinidSystem : EntitySystem
    {

        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FelinidComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<FelinidComponent, HairballActionEvent>(OnHairball);
        }

        private void OnInit(EntityUid uid, FelinidComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, component.HairballAction, uid);
        }

        private void OnHairball(EntityUid uid, FelinidComponent component, HairballActionEvent args)
        {
            SoundSystem.Play(Filter.Pvs(uid), "/Audio/Effects/Species/hairball.ogg", uid, AudioHelpers.WithVariation(0.15f));
            EntityManager.SpawnEntity(component.HairballPrototype, Transform(uid).Coordinates);
            args.Handled = true;
        }
    }

    public sealed class HairballActionEvent : InstantActionEvent {}
}
