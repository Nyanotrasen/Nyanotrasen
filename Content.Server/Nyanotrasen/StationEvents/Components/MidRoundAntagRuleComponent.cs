using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MidRoundAntagRule))]
public sealed class MidRoundAntagRuleComponent : Component
{

    [ViewVariables(VVAccess.ReadWrite), DataField("spawnPoint", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnPoint = String.Empty;

}
