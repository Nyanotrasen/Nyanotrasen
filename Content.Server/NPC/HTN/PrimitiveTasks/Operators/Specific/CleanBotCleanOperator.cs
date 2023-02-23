using Content.Server.Silicons.Bots;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators.Specific;

public sealed class CleanBotCleanOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private CleanBotSystem _cleanbotSystem = default!;

    [DataField("cleanKey")]
    public string CleanKey = string.Empty;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _cleanbotSystem = sysManager.GetEntitySystem<CleanBotSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var target = blackboard.GetValue<EntityUid>(CleanKey);

        if (!target.IsValid() || _entManager.Deleted(target))
            return HTNOperatorStatus.Failed;

        if (!_entManager.TryGetComponent<CleanBotComponent>(owner, out var cleanbot))
            return HTNOperatorStatus.Failed;

        if (cleanbot.CancelToken != null)
            return HTNOperatorStatus.Continuing;

        if (cleanbot.CleanTarget == null)
        {
            if (_cleanbotSystem.TryStartClean(owner, cleanbot, target))
                return HTNOperatorStatus.Continuing;
            else
                return HTNOperatorStatus.Failed;
        }

        cleanbot.CleanTarget = null;

        return HTNOperatorStatus.Finished;
    }
}
