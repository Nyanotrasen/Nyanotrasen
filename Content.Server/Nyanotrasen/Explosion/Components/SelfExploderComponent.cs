using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class SelfExploderComponent : Component
    {
        [DataField("explodeSelfAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
        public string ExplodeSelfAction = "ExplodeSelf";
    }
}
