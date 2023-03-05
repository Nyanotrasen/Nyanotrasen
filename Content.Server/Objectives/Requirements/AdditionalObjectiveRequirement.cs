using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.Objectives.Interfaces;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public sealed class AdditionalObjectiveRequirement : IObjectiveRequirement
    {
        [DataField("objective", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<ObjectivePrototype>))]
        private string _objectiveId = default!;

        /// <summary>
        /// This requirement is met if the additional objective can be added successfully or is already assigned.
        /// </summary>
        public bool CanBeAssigned(Mind.Mind mind)
        {
            foreach (var assignedObjective in mind.AllObjectives)
                if (assignedObjective.Prototype.ID == _objectiveId)
                    return true;

            var objective = IoCManager.Resolve<IPrototypeManager>().Index<ObjectivePrototype>(_objectiveId);
            return mind.TryAddObjective(objective);
        }
    }
}
