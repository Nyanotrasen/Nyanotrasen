using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Pathfinding;
using Content.Server.NPC.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

/// <summary>
/// Chooses a nearby coordinate and puts it into the resulting key.
/// </summary>
public sealed class PickEvasionPathOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private PathfindingSystem _pathfinding = default!;
    private SharedPhysicsSystem _physicsSystem = default!;

    [DataField("rangeKey", required: true)]
    public string RangeKey = string.Empty;

    [DataField("targetKey", required: true)]
    public string TargetKey = string.Empty;

    /// <summary>
    /// Where the pathfinding result will be stored (if applicable). This gets removed after execution.
    /// </summary>
    [DataField("pathfindKey")]
    public string PathfindKey = NPCBlackboard.PathfindKey;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _pathfinding = sysManager.GetEntitySystem<PathfindingSystem>();
        _physicsSystem = sysManager.GetEntitySystem<SharedPhysicsSystem>();
    }

    /// <inheritdoc/>
    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        // Very inefficient (should weight each region by its node count) but better than the old system
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<NPCCombatTargetComponent>(owner, out var targetComponent))
            return (false, null);

        if (targetComponent.EngagingEnemies.Count == 0)
        {
            _entManager.RemoveComponent<NPCCombatTargetComponent>(owner);
            return (false, null);
        }

        SortedList<float, EntityUid> engagers = new();

        foreach (var entity in targetComponent.EngagingEnemies)
        {
            if (!_physicsSystem.TryGetDistance(owner, entity, out float distance))
                continue;

            engagers.Add(distance, entity);
        }

        var runFrom = engagers.First().Value;

        if (!_entManager.TryGetComponent<TransformComponent>(owner, out var ourXform))
            return (false, null);

        if (!_entManager.TryGetComponent<TransformComponent>(runFrom, out var enemyXform))
            return (false, null);


        blackboard.TryGetValue<float>(RangeKey, out var maxRange, _entManager);

        if (maxRange == 0f)
            maxRange = 7f;

        var vector = enemyXform.Coordinates.Position - ourXform.Coordinates.Position;
        vector = vector.Normalized * 40f;
        vector = new Vector2(-vector.X, -vector.Y);

        var targetPos = ourXform.Coordinates.Offset(vector);

        var path = await _pathfinding.GetPath(owner, ourXform.Coordinates, targetPos, maxRange, cancelToken);


        if (path.Result != PathResult.Path)
        {
            return (false, null);
        }

        var target = path.Path.Last().Coordinates;

        return (true, new Dictionary<string, object>()
        {
            { TargetKey, target },
            { PathfindKey, path}
        });
    }
}
