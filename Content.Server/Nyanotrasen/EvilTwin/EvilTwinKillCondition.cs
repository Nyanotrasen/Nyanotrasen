using Content.Server.Objectives.Interfaces;
using Content.Server.EvilTwin;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class EvilTwinKillCondition : KillPersonCondition
    {
        public override IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent<EvilTwinComponent>(mind.OwnedEntity, out var twin))
                return new EscapeShuttleCondition();

            if (twin.TwinMind == null)
                return new DieCondition();

            return new EvilTwinKillCondition {Target = twin.TwinMind};
        }
    }
}
