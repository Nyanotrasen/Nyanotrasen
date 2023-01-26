using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Server.Player;

namespace Content.Server.Redial
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class SendRedialToOtherCommand : IConsoleCommand
    {
        public string Command => "redialother";
        public string Description => Loc.GetString("command-redialother-description");
        public string Help => Loc.GetString("command-redialother-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError("Need at least one argument");
                return;
            }

            var playerName = args[0];

            var players = IoCManager.Resolve<IPlayerManager>();

            if (!players.TryGetSessionByUsername(playerName, out var session))
            {
                shell.WriteError("That username is not connected.");
                return;
            }

            var redial = IoCManager.Resolve<RedialManager>();

            redial.SendRedialMessage(session, args[1]);
        }
    }
}
