using Content.Shared.Actions;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Abilities.Fart
{
    public sealed class FartSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FarterComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<FarterComponent, FartActionEvent>(OnFart);
        }

        private void OnInit(EntityUid uid, FarterComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, component.FartAction, uid);
        }

        private void OnFart(EntityUid uid, FarterComponent component, FartActionEvent args)
        {
            SoundSystem.Play(Filter.Pvs(uid), component.FartSound.GetSound(), uid, AudioHelpers.WithVariation(0.3f));
            args.Handled = true;
        }
    }

    public sealed class FartActionEvent : InstantActionEvent {}
}
