using Robust.Shared.Prototypes;

namespace Content.Server.AI.Utility
{
    [Prototype("behaviorSet")]
    public sealed class BehaviorSetPrototype : IPrototype
    {
        /// <summary>
        ///     Name of the BehaviorSet.
        /// </summary>
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        /// <summary>
        ///     Actions that this BehaviorSet grants to the entity.
        /// </summary>
        [DataField("actions")]
        public IReadOnlyList<string> Actions { get; private set; } = new List<string>();
    }
}
