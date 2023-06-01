using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MidRoundAntagRule))]
public sealed class MidRoundAntagRuleComponent : Component
{
    [DataField("antags")]
    public readonly IReadOnlyList<string> MidRoundAntags = new[]
    {
        "SpawnPointGhostRatKing",
        "SpawnPointGhostVampSpider",
        "SpawnPointGhostFugitive",
        "MobEvilTwinSpawn"
    };
}
