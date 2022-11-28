using System.Linq;
using Content.Server.Administration;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Administration;
using Content.Shared.Borgs;
using Robust.Shared.Console;
using Content.Server.Players;
using Robust.Server.Player;

namespace Content.Server.Borgs;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListLawsCommand : IConsoleCommand
{
    public string Command => "lslaws";
    public string Description => Loc.GetString("command-lslaws-description");
    public string Help => Loc.GetString("command-lslaws-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var player = shell.Player as IPlayerSession;
        IPlayerData? data;
        if (args.Length == 0 && player != null)
        {
            data = player.Data;
        }
        else if (player == null || !IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out data))
        {
            shell.WriteLine("Can't find the playerdata.");
            return;
        }

        var entity = data.ContentData()?.Mind?.CurrentEntity;
        if (entity == null)
        {
            shell.WriteLine("Can't find entity.");
            return;
        }

        if (!entityManager.TryGetComponent<LawsComponent>(entity, out var laws))
        {
            shell.WriteLine("Entity has no laws.");
            return;
        }

        shell.WriteLine($"Laws for entity {entity}:");
        foreach (var law in laws.Laws)
        {
            shell.WriteLine(law);
        }
    }
}
