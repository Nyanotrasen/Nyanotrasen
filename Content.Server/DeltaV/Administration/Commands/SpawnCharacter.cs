using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Preferences;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.DeltaV.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class SpawnCharacter : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitySys = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IServerPreferencesManager _prefs = default!;

        public string Command => "spawncharacter";
        public string Description => Loc.GetString("spawncharacter-command-description");
        public string Help => Loc.GetString("spawncharacter-command-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteError(Loc.GetString("shell-only-players-can-run-this-command"));
                return;
            }

            var mind = player.ContentData()?.Mind;

            if (mind == null || mind.UserId == null)
            {
                shell.WriteError(Loc.GetString("shell-entity-is-not-mob"));
                return;
            }


            HumanoidCharacterProfile character;

            if (args.Length >= 1)
            {
                // This seems like a bad way to go about it, but it works so eh?
                var name = string.Join(" ", args.ToArray());
                shell.WriteLine(Loc.GetString("loadcharacter-command-fetching", ("name", name)));

                var charIndex = _prefs.GetPreferences(mind.UserId.Value).IndexOfCharacterName(name);
                if (charIndex < 0)
                {
                    shell.WriteError(Loc.GetString("loadcharacter-command-fetching-failed"));
                    return;
                }

                character = (HumanoidCharacterProfile) _prefs.GetPreferences(mind.UserId.Value).GetProfile(charIndex);
            }
            else
                character = (HumanoidCharacterProfile) _prefs.GetPreferences(mind.UserId.Value).SelectedCharacter;


            var coordinates = player.AttachedEntity != null
                ? _entityManager.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
                : _entitySys.GetEntitySystem<GameTicker>().GetObserverSpawnPoint();

            mind.TransferTo(_entityManager.System<StationSpawningSystem>()
                .SpawnPlayerMob(coordinates: coordinates, profile: character, entity: null, job: null, station: null));

            shell.WriteLine(Loc.GetString("spawncharacter-command-complete"));
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var player = shell.Player as IPlayerSession;
                if (player == null)
                    return CompletionResult.Empty;
                var mind = player.ContentData()?.Mind;
                if (mind == null || mind.UserId == null)
                    return CompletionResult.Empty;

                return CompletionResult.FromHintOptions(_prefs.GetPreferences(mind.UserId.Value).GetCharacterNames(), Loc.GetString("loadcharacter-command-hint-select"));
            }

            return CompletionResult.Empty;
        }
    }
}
