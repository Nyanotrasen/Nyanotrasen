using Robust.Shared.Console;

namespace Content.Client.Redial;

public sealed class RedialRandomCommand : IConsoleCommand
{
    public string Command => "redialrandom";
    public string Description => Loc.GetString("command-redialrandom-description");
    public string Help => Loc.GetString("command-redialrandom-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var redialSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RedialSystem>();

        redialSystem.TryRedialToRandom();
    }
}
