using Content.Server.Explosion.Components;
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Content.Shared.Inventory;
using Content.Shared.Hands;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Medical;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
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

        private void ExplodeSelf(EntityUid uid, SelfExploderComponent? component)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!_blocker.CanInteract(uid, null))
                return;

            _explosionSystem.TriggerExplosive(uid, user: uid);
        }
    }

    public sealed class ExplodeSelfActionEvent : InstantActionEvent {}
}
