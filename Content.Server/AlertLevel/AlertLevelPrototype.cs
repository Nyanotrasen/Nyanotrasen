using System.Collections.Specialized;
using Content.Shared.Sound;
using Robust.Shared.Prototypes;

namespace Content.Server.AlertLevel;

[Prototype("alertLevels")]
public sealed class AlertLevelPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Dictionary of alert levels. Keyed by string - the string key is the most important
    /// part here. Visualizers will use this in order to dictate what alert level to show on
    /// client side sprites, and localization uses each key to dictate the alert level name.
    /// </summary>
    [DataField("levels")] public Dictionary<string, AlertLevelDetail> Levels = new();

    /// <summary>
    /// Default level that the station is on upon initialization.
    /// If this isn't in the dictionary, this will default to whatever .First() gives.
    /// </summary>
    [DataField("defaultLevel")] public string DefaultLevel { get; }= default!;
}

/// <summary>
/// Alert level detail. Does not contain an ID, that is handled by
/// the Levels field in AlertLevelPrototype.
/// </summary>
[DataDefinition]
public sealed class AlertLevelDetail
{
    /// <summary>
    /// What is announced upon this alert level change. Can be a localized string.
    /// </summary>
    [DataField("announcement")] public string Announcement { get; } = string.Empty;

    /// <summary>
    /// Whether this alert level is selectable from a communications console.
    /// </summary>
    [DataField("selectable")] public bool Selectable { get; } = true;

    /// <summary>
    /// If this alert level disables user selection while it is active. Beware -
    /// setting this while something is selectable will disable selection permanently!
    /// This should only apply to entities or gamemodes that auto-select an alert level,
    /// such as a nuclear bomb being set to active.
    /// </summary>
    [DataField("disableSelection")] public bool DisableSelection { get; }

    /// <summary>
    /// The sound that this alert level will play in-game once selected.
    /// </summary>
    [DataField("sound")] public SoundSpecifier? Sound { get; }

    /// <summary>
    /// The color that this alert level will show in-game in chat.
    /// </summary>
    [DataField("color")] public Color Color { get; } = Color.White;
}

