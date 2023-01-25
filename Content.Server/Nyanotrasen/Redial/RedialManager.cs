using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using Content.Shared.CCVar;
using Content.Shared.Redial;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Players;

namespace Content.Server.Redial;

public class RedialManager
{
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly HttpClient _http = new();

    private TimeSpan _nextUpdateTime = TimeSpan.Zero;
    private TimeSpan _updateRate = TimeSpan.FromSeconds(30);
    private List<string> _validServers = new();
    public void Initialize()
    {
        _net.RegisterNetMessage<MsgRedialServer>();
        _nextUpdateTime = _timing.CurTime;
    }

    public void Update()
    {
        if (_timing.CurTime < _nextUpdateTime)
            return;

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

    private void OnRequestRedials(RequestRandomRedialServer request, EntitySessionEventArgs args)
    {
        if (_validServers.Count < 1)
            return;

        var msg = new MsgRedialServer
        {
            Server = _random.Pick(_validServers)
        };

        _net.ServerSendMessage(msg, args.SenderSession.ConnectedClient);
    }

    public string? GetRandomRedial()
    {
        if (_validServers.Count < 1)
            return null;

        var server = _random.Pick(_validServers);

        return server;
    }

    public bool RedialAvailable()
    {
        Logger.Error("Valid servers: " + _validServers.Count);
        return (_validServers.Count > 0);
    }

    public void SendRedialMessage(ICommonSession session, string? server = null)
    {
        if (server == null)
        {
            if (_validServers.Count < 1)
                return;
            else
                server = _random.Pick(_validServers);
        }

        var msg = new MsgRedialServer
        {
            Server = server
        };

        _net.ServerSendMessage(msg, session.ConnectedClient);
    }

    private async Task UpdateServer(string address)
    {
        var statusAddress = "http:" + address + "/status";

        var status = new ServerStatus(null, 0, 0);

        status = await _http.GetFromJsonAsync<ServerStatus>(statusAddress)
                    ?? throw new InvalidDataException();

        var ss14Address = "ss14:" + address;

        if (status.PlayerCount < status.SoftMaxPlayerCount - 5)
        {
            _validServers.Add(ss14Address);
        } else
        {
            _validServers.Remove(ss14Address);
        }
    }
}
