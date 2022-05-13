using Content.Shared.Access.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Access.Components
{
    /// <summary>
    ///     Simple mutable access provider found on ID cards and such.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(AccessSystem))]
    public sealed class AccessComponent : Component
    {
        [DataField("tags", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
        public HashSet<string> Tags = new();

        [DataField("groups", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessGroupPrototype>))]
        public HashSet<string> Groups = new();
    }
}
