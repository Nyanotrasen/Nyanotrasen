using Content.Server.AI.Operators;
using Content.Server.AI.Operators.Generic;
using Content.Server.AI.Operators.Movement;
using Content.Server.AI.WorldState;
using Content.Server.AI.Utility.Considerations.Movement;
using Content.Server.Fluids.Components;
using Content.Server.AI.Pathfinding.Accessible;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.Utility.Considerations.ActionBlocker;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.AI.Utility.Actions.Bots
{
    public sealed class GoToPuddleAndWait : UtilityAction
    {

        public override void SetupOperators(Blackboard context)
        {
            MoveToEntityOperator moveOperator = new MoveToEntityOperator(Owner, GetNearbyPuddle(Owner), 0, 0);
            float waitTime = 3f;

            ActionOperators = new Queue<AiOperator>(new AiOperator[]
            {
                moveOperator,
                new WaitOperator(waitTime),
            });
        }

        protected override IReadOnlyCollection<Func<float>> GetConsiderations(Blackboard context)
        {
            var considerationsManager = IoCManager.Resolve<ConsiderationsManager>();

            return new[]
            {
                considerationsManager.Get<CanMoveCon>()
                    .BoolCurve(context),
            };
        }

        private EntityUid GetNearbyPuddle(EntityUid cleanbot)
        {
            foreach (var entity in EntitySystem.Get<EntityLookupSystem>().GetEntitiesInRange(Owner, 10))
            {
                if (IoCManager.Resolve<IEntityManager>().HasComponent<PuddleComponent>(entity))
                    return entity;
            }

            return default;
        }
    }
}
