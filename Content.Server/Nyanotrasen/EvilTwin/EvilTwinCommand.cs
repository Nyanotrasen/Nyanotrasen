using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Borgs;
using Robust.Shared.Console;
using Content.Server.Players;
using Robust.Server.Player;

namespace Content.Server.EvilTwin;

[AdminCommand(AdminFlags.Fun)]
public sealed class EvilTwinCommand : IConsoleCommand
{
    public string Command => "eviltwin";
    public string Description => Loc.GetString("command-lawclear-description");
    public string Help => Loc.GetString("command-lawclear-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        entityManager.EntitySysManager.GetEntitySystem<EvilTwinSystem>().SpawnEvilTwin();
    }
}
