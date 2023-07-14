using Content.Server.Explosion.Components;
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed class SelfExplosionSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SelfExploderComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SelfExploderComponent, ExplodeSelfActionEvent>(OnAction);
        }

        private void OnInit(EntityUid uid, SelfExploderComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<InstantActionPrototype>(component.ExplodeSelfAction, out var action))
                return;

            _actionsSystem.AddAction(uid, new InstantAction(action), null);
        }

        private void OnAction(EntityUid uid, SelfExploderComponent component, ExplodeSelfActionEvent args)
        {
            ExplodeSelf(uid, component);
            args.Handled = true;
        }

        public bool ExplodeSelf(EntityUid uid, SelfExploderComponent? component)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!_blocker.CanInteract(uid, null))
                return false;

            _explosionSystem.TriggerExplosive(uid, user: uid);
            return true;
        }
    }

    public sealed class ExplodeSelfActionEvent : InstantActionEvent {}
}
