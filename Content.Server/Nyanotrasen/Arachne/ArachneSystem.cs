using Content.Shared.Arachne;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Server.Coordinates.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Player;


namespace Content.Server.Arachne
{
    public sealed class ArachneSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ArachneComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SpinWebActionEvent>(OnSpinWeb);
        }

        private void OnInit(EntityUid uid, ArachneComponent component, ComponentInit args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("SpinWeb", out var spinWeb))
                _actions.AddAction(uid, new InstantAction(spinWeb), null);
        }

        private void OnSpinWeb(SpinWebActionEvent args)
        {
            Spawn("ArachneWeb", Transform(args.Performer).Coordinates.SnapToGrid());
            args.Handled = true;
        }
    }
    public sealed class SpinWebActionEvent : InstantActionEvent {}
}
