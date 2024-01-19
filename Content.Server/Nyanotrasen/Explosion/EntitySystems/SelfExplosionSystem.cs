using Content.Server.Explosion.Components;
using Content.Server.Destructible;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed class SelfExplosionSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SelfExploderComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<SelfExploderComponent, ExplodeSelfActionEvent>(OnAction);
            SubscribeLocalEvent<SelfExploderComponent, DestructionEventArgs>(OnDestroyed);
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

        private void OnDestroyed(EntityUid uid, SelfExploderComponent component, DestructionEventArgs args)
        {
            Logger.Error("Dumping stuff out.");
            var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);

            if (environment != null)
                _atmosphereSystem.Merge(environment, component.Mixture);

            component.Mixture.Clear();
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
                Logger.Error("Trying to deal damage...");
                var limit = _destructibleSystem.DestroyedAt(uid);
                Logger.Error("Limit: " + limit);
                var damageVariance = _configurationManager.GetCVar(CCVars.DamageVariance);
                limit *= 1f + damageVariance;

                var smash = new DamageSpecifier();
                smash.DamageDict.Add("Blunt", limit);
                _damageableSystem.TryChangeDamage(uid, smash, ignoreResistances: true);
            }

            return true;
        }
    }

    public sealed class ExplodeSelfActionEvent : InstantActionEvent {}
}
