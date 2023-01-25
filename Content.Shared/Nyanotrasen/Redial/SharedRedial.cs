using System.Text.Json.Serialization;
using Robust.Shared.Serialization;
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Redial
{
    /// <summary>
    ///     A client request for servers to connect to.
    ///     Should this also be a net message? I dunno it's sending no actual info.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestRandomRedialServer : EntityEventArgs
    {
    }

    public sealed record ServerStatus(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("players")]
        int PlayerCount,
        [property: JsonPropertyName("soft_max_players")]
        int SoftMaxPlayerCount);
}
