using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Movement
{
    [UsedImplicitly]
    public sealed class MoveTargetState : PlanningStateData<EntityUid?>
    {
        public override string Name => "MoveTarget";
        public override void Reset()
        {
            Value = null;
        }
    }
}
