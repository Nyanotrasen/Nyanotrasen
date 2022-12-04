using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.StatusEffect;
using Content.Shared.Abilities.Psionics;
using Content.Server.Mind.Components;
using Content.Server.Visible;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Content.Shared.MobState;
using Content.Shared.MobState.EntitySystems;


namespace Content.Server.Abilities.Psionics
{
    public sealed class AITelegnosisPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly MindSwapPowerSystem _mindSwap = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<AITelegnosisPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<AITelegnosisPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<AITelegnosisPowerComponent, AITelegnosisPowerActionEvent>(OnPowerUsed);
            SubscribeLocalEvent<AITelegnosticProjectionComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<AITelegnosisPowerComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnInit(EntityUid uid, AITelegnosisPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<InstantActionPrototype>("AIeye", out var metapsionic))
                return;

            component.TelegnosisPowerAction = new InstantAction(metapsionic);
            _actions.AddAction(uid, component.TelegnosisPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic) && psionic.PsionicAbility == null)
                psionic.PsionicAbility = component.TelegnosisPowerAction;
        }

        private void OnShutdown(EntityUid uid, AITelegnosisPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("AIeye", out var metapsionic))
                _actions.RemoveAction(uid, new InstantAction(metapsionic), null);
        }

        private void OnPowerUsed(EntityUid uid, AITelegnosisPowerComponent component, AITelegnosisPowerActionEvent args)
        {
            var projection = Spawn(component.Prototype, Transform(uid).Coordinates);
            _mindSwap.Swap(uid, projection);

            _psionics.LogPowerUsed(uid, "aieye");
            args.Handled = true;
        }
        private void OnMindRemoved(EntityUid uid, AITelegnosticProjectionComponent component, MindRemovedMessage args)
        {
            QueueDel(uid);
        }

        private void OnMobStateChanged(EntityUid uid, AITelegnosisPowerComponent component, MobStateChangedEvent args)
        {
            foreach (var projection in _entityManager.EntityQuery<AITelegnosticProjectionComponent>(true))
            {
                if (args.CurrentMobState is not DamageState.Dead) continue;

                TryComp<MindSwappedComponent>(projection.Owner, out var mindSwapped);
                if (mindSwapped == null) continue;

                _mindSwap.Swap(projection.Owner, mindSwapped.OriginalEntity);
                // QueueDel(projection.Owner);
            }
        }
    }

    public sealed class AITelegnosisPowerActionEvent : InstantActionEvent { }
}
