using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ReverseEngineering;

[Serializable, NetSerializable]
public enum ReverseEngineeringMachineUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineScanButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineSafetyButtonToggledMessage : BoundUserInterfaceMessage
{
    public bool Safety;

    public ReverseEngineeringMachineSafetyButtonToggledMessage(bool safety)
    {
        Safety = safety;
    }
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineAutoScanButtonToggledMessage : BoundUserInterfaceMessage
{
    public bool AutoScan;

    public ReverseEngineeringMachineAutoScanButtonToggledMessage(bool autoScan)
    {
        AutoScan = autoScan;
    }
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineScanUpdateState : BoundUserInterfaceState
{
    public EntityUid? Target;

    public bool CanScan;

    public FormattedMessage? ScanReport;

    public bool Scanning;

    public int TotalProgress;

    public TimeSpan TimeRemaining;

    public TimeSpan TotalTime;

    public ReverseEngineeringMachineScanUpdateState(EntityUid? target, bool canScan,
        FormattedMessage? scanReport, bool scanning, int totalProgress, TimeSpan timeRemaining, TimeSpan totalTime)
    {
        Target = target;
        CanScan = canScan;

        ScanReport = scanReport;

        Scanning = scanning;
        TotalProgress = totalProgress;
        TimeRemaining = timeRemaining;
        TotalTime = totalTime;
    }
}

/// <summary>
// 3d6 + scanner bonus + danger bonus - item difficulty
/// </summary>
[Serializable, NetSerializable]
public enum ReverseEngineeringTickResult : byte
{
    Destruction, // 8 (only destroys if danger bonus is active)
    Stagnation, // 9-10
    SuccessMinor, // 11-12
    SuccessAverage, // 13-15
    SuccessMajor, // 16-17
    InstantSuccess // 18
}
