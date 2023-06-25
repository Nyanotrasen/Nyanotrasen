using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Server.StationEvents;
using Content.Shared.Damage;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(DeathByStarvationRuleSystem))]
public sealed class DeathByStarvationRuleComponent : Component
{
    [ViewVariables]
    [DataField("nextTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick = TimeSpan.Zero;

    [ViewVariables]
    [DataField("interval")]
    public TimeSpan TickInterval = TimeSpan.FromSeconds(5);

    [ViewVariables]
    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;

    [ViewVariables]
    [DataField("alertProbability")]
    public float AlertProbability = 0.3f;

    [ViewVariables]
    [DataField("alert")]
    public string Alert = "rule-death-by-starvation-alert";
}
