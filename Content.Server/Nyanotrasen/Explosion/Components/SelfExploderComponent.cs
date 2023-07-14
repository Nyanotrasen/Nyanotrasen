using Content.Shared.Actions.ActionTypes;
using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class SelfExploderComponent : Component
    {
        [DataField("explodeSelfAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public string ExplodeSelfAction = "ExplodeSelf";

        /// <summary>
        /// Self damage, enough and of the correct type to trigger destruction.
        /// </summary>
        [DataField("selfDamage", required:true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier SelfDamage = default!;
    }
}
