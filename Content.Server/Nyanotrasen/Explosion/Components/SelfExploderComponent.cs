using Content.Shared.Actions.ActionTypes;
using Content.Server.Atmos;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class SelfExploderComponent : Component
    {
        /// <summary>
        /// The air inside this entity.
        /// Why yes, there is no generic gas mixture container component right now.
        /// </summary>
        [DataField("gasMixture"), ViewVariables(VVAccess.ReadWrite)]
        public GasMixture Mixture { get; } = new();

        [DataField("explodeSelfAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public string ExplodeSelfAction = "ExplodeSelf";
    }
}
