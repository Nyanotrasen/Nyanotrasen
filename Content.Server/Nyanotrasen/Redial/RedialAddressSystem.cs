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

namespace Content.Server.Redial;

public class RedialSystem : EntitySystem
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
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RequestRedialServersMessage>(OnRequestRedials);
        _net.RegisterNetMessage<MsgRedialServer>();
        _nextUpdateTime = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
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

    private void OnRequestRedials(RequestRedialServersMessage request, EntitySessionEventArgs args)
    {
        Logger.Error("Valid servers: " + _validServers.Count);
        if (_validServers.Count < 1)
            return;

        foreach (var server in _validServers)
        {
            Logger.Error("Address: " + server);
        }

        var msg = new MsgRedialServer
        {
            Server = _random.Pick(_validServers)
        };

        Logger.Error("sending message to connect to server: " + msg.Server);

        _net.ServerSendMessage(msg, args.SenderSession.ConnectedClient);
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

        var ss14Address = "ss14:" + address;

        if (status.PlayerCount < status.SoftMaxPlayerCount)
        {
            _validServers.Add(ss14Address);
        } else
        {
            _validServers.Remove(ss14Address);
        }
    }
}
