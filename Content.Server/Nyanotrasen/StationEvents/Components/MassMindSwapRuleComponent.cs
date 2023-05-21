using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MassMindSwapRule))]
public sealed class MassMindSwapRuleComponent : Component
{
    /// <summary>
    /// The mind swap is only temporary if true.
    /// </summary>
    [DataField("isTemporary")]
    public bool IsTemporary;
}
