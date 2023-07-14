using Content.Server.Explosion.Components;
using Content.Server.Destructible;
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed class SelfExplosionSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

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

            // If they have no special destruction behavior, just explode directly
            if (!HasComp<DestructibleComponent>(uid))
            {
                _explosionSystem.TriggerExplosive(uid, user: uid);
            }
            // yeah this kind of sucks but destructible sucks
            else
            {
                _damageableSystem.TryChangeDamage(uid, component.SelfDamage, true);
            }

            return true;
        }
    }

    public sealed class ExplodeSelfActionEvent : InstantActionEvent {}
}
