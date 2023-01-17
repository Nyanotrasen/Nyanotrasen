using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ReverseEngineering;

/// <summary>
/// This machine can reverse engineer items and get a technology disk from them.
/// </summary>
[RegisterComponent]
public sealed partial class ReverseEngineeringMachineComponent : Component
{
    [DataField("diskPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DiskPrototype = "TechnologyDisk";
}
