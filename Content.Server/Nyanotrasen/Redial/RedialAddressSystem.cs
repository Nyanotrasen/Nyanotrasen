using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.IO;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;

namespace Content.Server.Redial;

public class RedialSystem : EntitySystem
{
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly HttpClient _http = new();

    private TimeSpan _nextUpdateTime = TimeSpan.Zero;
    private TimeSpan _updateRate = TimeSpan.FromSeconds(30);
    private List<String> _validServers = new();
    public override void Initialize()
    {
        base.Initialize();
        _nextUpdateTime = _timing.CurTime + TimeSpan.FromSeconds(15);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _nextUpdateTime)
            return;

        _validServers.Clear();

        var path = _cfg.GetCVar(CCVars.RedialAddressesFile);
        try
        {
            var addresses = _res.ContentFileReadAllText($"/Server Info/{path}").Split("\n");
            foreach (var address in addresses)
            {
                if (address.StartsWith("//"))
                    UpdateServer(address);
            }
        }
        catch (Exception)
        {
            Logger.ErrorS("info", "Could not read server redial addresses file.");
        }

        _nextUpdateTime = _timing.CurTime + _updateRate;
    }

    private async Task UpdateServer(string address)
    {
        var statusAddress = "http:" + address + "/status";

        var status = new ServerStatus(null, 0, 0);

        Logger.Error("Getting status for " + statusAddress);
        status = await _http.GetFromJsonAsync<ServerStatus>(statusAddress)
                    ?? throw new InvalidDataException();

        Logger.Error("Name: " + status.Name);
        Logger.Error("Max players: " + status.SoftMaxPlayerCount);
        Logger.Error("Current players: " + status.PlayerCount);

        if (status.PlayerCount < status.SoftMaxPlayerCount)
        {
            _validServers.Add(address);
        }
    }

    private sealed record ServerStatus(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("players")]
        int PlayerCount,
        [property: JsonPropertyName("soft_max_players")]
        int SoftMaxPlayerCount);
}
