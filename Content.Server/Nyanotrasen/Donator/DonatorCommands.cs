using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Nyanotrasen.Donator;

[AdminCommand(AdminFlags.Host)]
public sealed class AddDonatorCommand : IConsoleCommand
{
    public string Command => "donatoradd";
    public string Description => Loc.GetString("command-donatoradd-description");
    public string Help => Loc.GetString("command-donatoradd-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is > 2 or < 1)
            return;

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = args[0];
        var data = await loc.LookupIdByNameAsync(name);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("command-donatoradd-not-found", ("name", name)));
            return;
        }

        uint days = 0;
        switch (args.Length)
        {
            case 2 when uint.TryParse(args[1], out days):
                break;
            case 2 when !uint.TryParse(args[1], out days):
                shell.WriteLine(Loc.GetString("command-donatoradd-invalid-time", ("time", args[1])));
                return;
        }

        DateTime? expires = null;
        if (days > 0)
        {
            expires = DateTime.Now.AddDays(days);
        }

        var guid = data.UserId;
        var isDonator = await db.GetDonatorStatusAsync(guid);

        if (isDonator)
        {
            shell.WriteLine(Loc.GetString("command-donatoradd-existing", ("name", name)));
            return;
        }

        await db.AddDonatorAsync(guid, expires);

        shell.WriteLine(Loc.GetString("command-donatoradd-added", ("name", name)));
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class RemoveDonatorCommand : IConsoleCommand
{
    public string Command => "donatorremove";
    public string Description => Loc.GetString("command-donatorremove-description");
    public string Help => Loc.GetString("command-donatorremove-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = args[0];
        var data = await loc.LookupIdByNameAsync(name);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("command-donatorremove-not-found", ("name", name)));
            return;
        }

        var guid = data.UserId;
        var isDonator = await db.GetDonatorStatusAsync(guid);

        if (!isDonator)
        {
            shell.WriteLine(Loc.GetString("command-donatorremove-existing", ("name", name)));
            return;
        }

        await db.RemoveDonatorAsync(guid);

        shell.WriteLine(Loc.GetString("command-donatorremove-removed", ("name", name)));
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class GetDonatorCommand : IConsoleCommand
{
    public string Command => "donatorget";
    public string Description => Loc.GetString("command-donatorget-description");
    public string Help => Loc.GetString("command-donatorget-help");

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();

        var name = args[0];
        var data = await loc.LookupIdByNameAsync(name);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("command-donatorget-not-found", ("name", name)));
            return;
        }

        var guid = data.UserId;
        var isDonator = await db.GetDonatorStatusAsync(guid);

        if (!isDonator)
        {
            shell.WriteLine(Loc.GetString("command-donatorget-not-donator", ("name", name)));
            return;
        }

        shell.WriteLine(Loc.GetString("command-donatorget-donator", ("name", name)));
    }
}
