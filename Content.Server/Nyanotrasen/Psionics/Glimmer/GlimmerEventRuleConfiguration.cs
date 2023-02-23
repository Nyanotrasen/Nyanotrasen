using JetBrains.Annotations;
using Content.Server.GameTicking.Rules.Configurations;

namespace Content.Server.Psionics.Glimmer;

/// <summary>
///     Defines a configuration for a given station event game rule, since all station events are just
///     game rules.
/// </summary>
[UsedImplicitly]
public sealed class GlimmerEventRuleConfiguration : GameRuleConfiguration
{
    [DataField("id", required: true)]
    private string _id = default!;
    public override string Id => _id;

    public const float WeightVeryLow = 0.0f;
    public const float WeightLow = 5.0f;
    public const float WeightNormal = 10.0f;
    public const float WeightHigh = 15.0f;
    public const float WeightVeryHigh = 20.0f;

    [DataField("weight")]
    public float Weight = WeightNormal;

    /// <summary>
    ///     Minimum glimmer value for event to be eligible. (Should be 100 at lowest.)
    /// </summary>
    [DataField("minimumGlimmer")]
    public int MinimumGlimmer = 100;

    /// <summary>
    ///     Maximum glimmer value for event to be eligible. (Remember 1000 is max glimmer period.)
    /// </summary>
    [DataField("maximumGlimmer")]
    public int MaximumGlimmer = 1000;

    /// <summary>
    ///     Will be used for _random.Next and subtracted from glimmer.
    ///     Lower bound.
    /// </summary>
    [DataField("glimmerBurnLower")]
    public int GlimmerBurnLower = 25;

    /// <summary>
    ///     Will be used for _random.Next and subtracted from glimmer.
    ///     Upper bound.
    /// </summary>
    [DataField("glimmerBurnUpper")]
    public int GlimmerBurnUpper = 70;

    /// <summary>
    ///     When in the lifetime to start the event.
    /// </summary>
    [DataField("startAfter")]
    public float StartAfter;

    /// <summary>
    ///     When in the lifetime to end the event..
    /// </summary>
    [DataField("endAfter")]
    public float EndAfter = float.MaxValue;

    [DataField("report")]
    public string SohpicReport = "glimmer-event-report-generic";
}
