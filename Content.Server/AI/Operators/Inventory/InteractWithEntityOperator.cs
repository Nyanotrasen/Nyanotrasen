using Content.Server.CombatMode;
using Content.Server.Interaction;

namespace Content.Server.AI.Operators.Inventory
{
    /// <summary>
    /// A Generic interacter; if you need to check stuff then make your own
    /// </summary>
    public sealed class InteractWithEntityOperator : AiOperator
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private readonly EntityUid _owner;
        private readonly EntityUid _useTarget;

        public InteractWithEntityOperator(EntityUid owner, EntityUid useTarget)
        {
            IoCManager.InjectDependencies(this);

            _owner = owner;
            _useTarget = useTarget;

        }

        public override Outcome Execute(float frameTime)
        {
            var targetTransform = _entMan.GetComponent<TransformComponent>(_useTarget);

            if (targetTransform.GridID != _entMan.GetComponent<TransformComponent>(_owner).GridID)
            {
                return Outcome.Failed;
            }

            var interactionSystem = EntitySystem.Get<InteractionSystem>();

            if (!interactionSystem.InRangeUnobstructed(_owner, _useTarget, popup: true))
            {
                return Outcome.Failed;
            }

            if (_entMan.TryGetComponent(_owner, out CombatModeComponent? combatModeComponent))
            {
                combatModeComponent.IsInCombatMode = false;
            }

            // Click on da thing
            interactionSystem.AiUseInteraction(_owner, targetTransform.Coordinates, _useTarget);

            return Outcome.Success;
        }
    }
}
