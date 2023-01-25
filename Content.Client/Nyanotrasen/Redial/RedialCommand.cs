using Robust.Shared.Console;

namespace Content.Client.Redial;

public sealed class RedialRandomCommand : IConsoleCommand
{
    public string Command => "redialrandom";
    public string Description => Loc.GetString("redial to a random approved server");
    public string Help => Loc.GetString("redial to a random approved server");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var redialSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<RedialSystem>();

        redialSystem.TryRedialToRandom();
    }
}
