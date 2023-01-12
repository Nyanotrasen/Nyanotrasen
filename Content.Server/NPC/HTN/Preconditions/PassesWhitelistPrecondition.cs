using Content.Shared.Whitelist;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Checks if the owner is being pulled or not.
/// </summary>
public sealed class PassesWhitelistPrecondition : HTNPrecondition
{
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (Whitelist == null)
            return false;

        var valid = Whitelist.IsValid(owner);

        return Whitelist.IsValid(owner);
    }
}
