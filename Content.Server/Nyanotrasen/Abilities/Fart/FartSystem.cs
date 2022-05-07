using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Maps;
using Content.Shared.Body.Components;
using Content.Server.Bible.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Containers;

namespace Content.Server.Abilities.Fart
{
    public sealed class FartSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
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
            if (TryComp<SharedBodyComponent>(uid, out var body))
            {
                foreach (var entity in Transform(uid).Coordinates.GetEntitiesInTile())
                {
                    if (HasComp<BibleComponent>(entity) && !_containerSystem.IsEntityInContainer(entity))
                    {
                        body.Gib();
                        args.Handled = true;
                        return;
                    }
                }
            }
            args.Handled = true;
        }
    }

    public sealed class FartActionEvent : InstantActionEvent {}
}
