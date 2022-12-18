﻿using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// A machine that is combined and linked to the <see cref="AnalysisConsoleComponent"/>
/// in order to analyze and destroy artifacts.
/// </summary>
[RegisterComponent]
public sealed class ArtifactAnalyzerComponent : Component
{
    /// <summary>
    /// How long it takes to analyze an artifact
    /// </summary>
    [DataField("analysisDuration", customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan AnalysisDuration = TimeSpan.FromSeconds(60);

    /// <summary>
    /// A mulitplier on the duration of analysis.
    /// Used for machine upgrading.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AnalysisDurationMulitplier = 1;

    /// <summary>
    /// The machine part that modifies analysis duration.
    /// </summary>
    [DataField("machinePartAnalysisDuration", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartAnalysisDuration = "ScanningModule";

    /// <summary>
    /// The modifier raised to the part rating to determine the duration multiplier.
    /// </summary>
    [DataField("partRatingAnalysisDurationMultiplier")]
    public float PartRatingAnalysisDurationMultiplier = 0.75f;

    /// <summary>
    /// Ratio of research points to glimmer.
    /// Each is 150 and added to this, so
    /// 550 / 700 / 850 / 1000
    /// </summary>
    public int SacrificeRatio = 400;

    /// <summary>
    // The machine part that modifies the sacrifice ratio.
    /// </summary>
    [DataField("machinePartSacrificeRatio", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartSacrificeRatio = "Manipulator";

    /// <summary>
    /// How many points per glimmer are added to the sacrifice ratio per tier.
    /// </summary>
    public int PartRatingSacrificeRatioMultiplier = 150;

    /// <summary>
    /// The corresponding console entity.
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables]
    public EntityUid? Console;

    /// <summary>
    /// All of the valid artifacts currently touching the analyzer.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Contacts = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ReadyToPrint = false;

    [DataField("scanFinishedSound")]
    public readonly SoundSpecifier ScanFinishedSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    #region Analysis Data
    [ViewVariables]
    public EntityUid? LastAnalyzedArtifact;

    [ViewVariables]
    public ArtifactNode? LastAnalyzedNode;

    [ViewVariables(VVAccess.ReadWrite)]
    public int? LastAnalyzerPointValue;
    #endregion
}
