using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(GlimmerRevenantRule))]
public sealed class GlimmerRevenantRuleComponent : Component
{
    [DataField("prototype")]
    public readonly string RevenantPrototype = "MobRevenant";
}
