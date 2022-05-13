using Content.Server.Botany.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components;
// TODO: This should probably be merged with SliceableFood somehow or made into a more generic Choppable.

[RegisterComponent]
[Friend(typeof(LogSystem))]
public sealed class LogComponent : Component
{
    [DataField("spawnedPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnedPrototype = "MaterialWoodPlank1";

    [DataField("spawnCount")] public int SpawnCount = 2;
}
