using Content.Server.Administration;
using Content.Server.Shipyard.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Shipyard.Commands;

/// <summary>
/// Sells a shuttle docked to a station.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class SellShuttleCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entityManager = default!;

    public string Command => "sellshuttle";
    public string Description => "Appraises and sells a selected grid connected to selected station";
    public string Help => $"{Command} <station ID> <shuttle ID>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!EntityUid.TryParse(args[0], out var stationId))
        {
            shell.WriteError($"{args[0]} is not a valid entity uid.");
            return;
        }

        if (!EntityUid.TryParse(args[1], out var shuttleId))
        {
            shell.WriteError($"{args[0]} is not a valid entity uid.");
            return;
        };

        var system = _entityManager.GetEntitySystem<ShipyardSystem>();
        system.SellShuttle(stationId, shuttleId, out _);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHint(Loc.GetString("station-id"));
            case 2:
                return CompletionResult.FromHint(Loc.GetString("shuttle-id"));
        }

        return CompletionResult.Empty;
    }
}
