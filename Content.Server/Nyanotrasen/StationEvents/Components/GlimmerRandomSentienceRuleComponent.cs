using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(GlimmerRandomSentienceRule))]
public sealed class GlimmerRandomSentienceRuleComponent : Component
{
    [DataField("maxMakeSentient")]
    public int MaxMakeSentient = 4;
}
