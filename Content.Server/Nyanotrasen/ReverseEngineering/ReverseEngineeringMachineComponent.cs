using Content.Shared.ReverseEngineering;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
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
    /// The machine part that affects cloning speed
    /// </summary>
    [DataField("machinePartScanBonus", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartScanBonus = "ScanningModule";

    /// <summary>
    /// Added to the 3d6, scales off of scanner.
    /// </summary>
    public int ScanBonus = 1;

    public EntityUid? CurrentItem;

    /// <summary>
    /// Malus from the item's difficulty.
    /// </summary>
    public int CurrentItemDifficulty = 0;

    /// <summary>
    /// Whether the machine is going to receive the danger bonus.
    /// </summary>
    [DataField("dangerBonus")]
    public int DangerBonus = 3;

    /// <summary>
    /// Whether the safety is on.
    /// </summary>
    public bool SafetyOn = true;

    /// <summary>
    /// Whether autoscan is on.
    /// </summary>
    public bool AutoScan = true;

    public int Progress = 0;

    public TimeSpan AnalysisDuration = TimeSpan.FromSeconds(30);

    public FormattedMessage? CachedMessage;

    public ReverseEngineeringTickResult? LastResult;
}
