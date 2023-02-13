using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Patreon;

[AdminCommand(AdminFlags.Host)]
public sealed class AddPatronCommand : IConsoleCommand
{
    public string Command => "patronadd";
    public string Description => Loc.GetString("command-patronadd-description");
    public string Help => Loc.GetString("command-patronadd-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = args[0];
        var data = await loc.LookupIdByNameAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isPatron = await db.GetPatronStatusAsync(guid);
            if (isPatron)
            {
                shell.WriteLine(Loc.GetString("command-patronadd-existing", ("username", data.Username)));
                return;
            }

            await db.AddPatronAsync(guid);

            shell.WriteLine(Loc.GetString("command-patronadd-added", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("command-patronadd-not-found", ("username", args[0])));
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class RemovePatronCommand : IConsoleCommand
{
    public string Command => "patronremove";
    public string Description => Loc.GetString("command-patronremove-description");
    public string Help => Loc.GetString("command-patronremove-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = args[0];
        var data = await loc.LookupIdByNameAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isPatron = await db.GetPatronStatusAsync(guid);
            if (!isPatron)
            {
                shell.WriteLine(Loc.GetString("command-patronremove-existing", ("username", data.Username)));
                return;
            }

            await db.RemovePatronAsync(guid);

            shell.WriteLine(Loc.GetString("command-patronremove-removed", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("command-patronremove-not-found", ("username", args[0])));
    }
}
