using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(NoosphericStormRule))]
public sealed class NoosphericStormRuleComponent : Component
{
    /// <summary>
    /// How many potential psionics should be awakened at most.
    /// </summary>
    [DataField("maxAwaken")]
    public readonly int MaxAwaken = 3;

    /// <summary>
    /// </summary>
    [DataField("baseGlimmerAddMin")]
    public readonly int BaseGlimmerAddMin = 65;

    /// <summary>
    /// </summary>
    [DataField("baseGlimmerAddMax")]
    public readonly int BaseGlimmerAddMax = 85;

    /// <summary>
    /// Multiply the EventSeverityModifier by this to determine how much extra glimmer to add.
    /// </summary>
    [DataField("glimmerSeverityCoefficient")]
    public readonly float GlimmerSeverityCoefficient = 0.25f;
}
