using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.IO;
using Content.Shared.CCVar;
using Content.Shared.Redial;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Players;
using Robust.Shared;

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
        var ours = _cfg.GetCVar(CVars.HubServerUrl);
        try
        {
            var addresses = _res.ContentFileReadAllText($"/Server Info/{path}").Split("\n");
            foreach (var address in addresses)
            {
                if (address == ours)
                    continue;

                // so far it seems like ss14: and ss14s: are the 2 valid ones.
                // if we get a third URI scheme this probably needs updating.
                if (address.StartsWith("ss14"))
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
        var statusAddress = "http://" + address.Split("//")[1] + "/status";

        var status = new ServerStatus(null, 0, 0);

        var cancel = new CancellationToken();

        try
        {
            using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                linkedToken.CancelAfter(TimeSpan.FromSeconds(10));

                status = await _http.GetFromJsonAsync<ServerStatus>(statusAddress, linkedToken.Token)
                            ?? throw new InvalidDataException();
            }

            cancel.ThrowIfCancellationRequested();
        }
        catch
        {
            _validServers.Remove(address);
            return;
        }

        // Current playercount standards:
        // 1. At least 3 players (we don't want to redirect people to a literally empty server, but we'll help seed)
        // 2. Less than 93% full when rounded (that number works well with all the common player limits)
        if (status.PlayerCount < Math.Round((float) status.SoftMaxPlayerCount * 0.93)
            && status.PlayerCount >= 3)
        {
            _validServers.Add(address);
        } else
        {
            _validServers.Remove(address);
        }
    }
}
