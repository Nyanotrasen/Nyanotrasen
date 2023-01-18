using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ReverseEngineering;

[Serializable, NetSerializable]
public enum ReverseEngineeringMachineUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineServerSelectionMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineScanButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachinePrintButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ReverseEngineeringMachineScanUpdateState : BoundUserInterfaceState
{
    public EntityUid? Artifact;

    public bool AnalyzerConnected;

    public bool ServerConnected;

    public bool CanScan;

    public bool CanPrint;

    public FormattedMessage? ScanReport;

    public bool Scanning;

    public TimeSpan TimeRemaining;

    public TimeSpan TotalTime;

    public ReverseEngineeringMachineScanUpdateState(EntityUid? artifact, bool analyzerConnected, bool serverConnected, bool canScan, bool canPrint,
        FormattedMessage? scanReport, bool scanning, TimeSpan timeRemaining, TimeSpan totalTime)
    {
        Artifact = artifact;
        AnalyzerConnected = analyzerConnected;
        ServerConnected = serverConnected;
        CanScan = canScan;
        CanPrint = canPrint;

        ScanReport = scanReport;

        Scanning = scanning;
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
