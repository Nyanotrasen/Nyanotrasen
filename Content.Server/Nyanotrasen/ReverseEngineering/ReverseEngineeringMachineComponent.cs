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

    /// <summary>
    /// Added to the 3d6, scales off of scanner.
    /// </summary>
    public int ScanBonus = 1;

    /// <summary>
    /// Malus from the item's difficulty.
    /// </summary>
    public int CurrentItemDifficulty = 1;

    /// <summary>
    /// Whether the machine is going to receive the danger bonus.
    /// </summary>
    public int DangerBonus = 0;
}
