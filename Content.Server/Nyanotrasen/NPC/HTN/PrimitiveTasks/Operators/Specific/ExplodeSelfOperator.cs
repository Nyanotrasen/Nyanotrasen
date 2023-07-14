using Content.Server.Explosion.EntitySystems;
using Content.Server.Explosion.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed class ExplodeSelfOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private SelfExplosionSystem _selfExplosionSystem = default!;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _selfExplosionSystem = sysManager.GetEntitySystem<SelfExplosionSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<SelfExploderComponent>(owner, out var exploder))
            return HTNOperatorStatus.Failed;

        if (!_selfExplosionSystem.ExplodeSelf(owner, exploder))
            return HTNOperatorStatus.Failed;

        return HTNOperatorStatus.Finished;
    }
}
