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
        EntityUid? entity = null;

        if (args.Length == 0 && player != null)
        {
            entity = player.ContentData()?.Mind?.CurrentEntity;
        }
        else if (IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out var data))
        {
            entity = data.ContentData()?.Mind?.CurrentEntity;
        }
        else if (EntityUid.TryParse(args[0], out var foundEntity))
        {
            entity = foundEntity;
        }

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

        shell.WriteLine($"Laws for {entityManager.ToPrettyString(entity.Value)}:");
        foreach (var law in laws.Laws)
        {
            shell.WriteLine(law);
        }
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class ClearLawsCommand : IConsoleCommand
{
    public string Command => "lawclear";
    public string Description => Loc.GetString("command-lawclear-description");
    public string Help => Loc.GetString("command-lawclear-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var player = shell.Player as IPlayerSession;
        EntityUid? entity = null;

        if (args.Length == 0 && player != null)
        {
            entity = player.ContentData()?.Mind?.CurrentEntity;
        }
        else if (IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out var data))
        {
            entity = data.ContentData()?.Mind?.CurrentEntity;
        }
        else if (EntityUid.TryParse(args[0], out var foundEntity))
        {
            entity = foundEntity;
        }

        if (entity == null)
        {
            shell.WriteLine("Can't find entity.");
            return;
        }

        if (!entityManager.TryGetComponent<LawsComponent>(entity.Value, out var laws))
        {
            shell.WriteLine("Entity has no laws component to clear");
            return;
        }

        entityManager.EntitySysManager.GetEntitySystem<LawsSystem>().ClearLaws(entity.Value, laws);
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class AddLawCommand : IConsoleCommand
{
    public string Command => "lawadd";
    public string Description => Loc.GetString("command-lawadd-description");
    public string Help => Loc.GetString("command-lawadd-help");
    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var player = shell.Player as IPlayerSession;
        EntityUid? entity = null;

        if (args.Length != 2)
        {
            shell.WriteLine("Wrong number of arguments.");
            return;
        }

        if (IoCManager.Resolve<IPlayerManager>().TryGetPlayerDataByUsername(args[0], out var data))
        {
            entity = data.ContentData()?.Mind?.CurrentEntity;
        }
        else if (EntityUid.TryParse(args[0], out var foundEntity))
        {
            entity = foundEntity;
        }

        if (entity == null)
        {
            shell.WriteLine("Can't find entity.");
            return;
        }

        var laws = entityManager.EnsureComponent<LawsComponent>(entity.Value);
        entityManager.EntitySysManager.GetEntitySystem<LawsSystem>().AddLaw(entity.Value, args[1], laws);
    }
}
