using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks the state of the owner's hunger.
/// </summary>
public sealed class HungerPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private HungerSystem _hunger = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("threshold")]
    public HungerThreshold Threshold = HungerThreshold.Peckish;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _hunger = sysManager.GetEntitySystem<HungerSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entManager.TryGetComponent<HungerComponent>(owner, out var hunger))
            return false;

        return _hunger.GetHungerThreshold(hunger) <= Threshold;
    }
}
