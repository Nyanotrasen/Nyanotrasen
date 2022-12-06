using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Guardian;
using Content.Server.Players;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Console;
namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class Brief : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public string Command => "brief";
        public string Description => "Makes you a centcomm officer.";
        public string Help => "brief";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteLine("Nah");
                return;
            }

            var mind = player.ContentData()?.Mind;

            if (mind == null)
            {
                shell.WriteLine("You can't spawn here!");
                return;
            }

            if (mind.VisitingEntity != default)
            {
                var entity = mind.VisitingEntity;
                player.ContentData()!.Mind?.UnVisit();

                foreach (var officer in _entities.EntityQuery<BriefOfficerComponent>(true))
                {
                    if (officer.Owner == entity) _entities.QueueDeleteEntity(officer.Owner);
                }
                return;
            }

            var coordinates = player.AttachedEntity != null
                ? _entities.GetComponent<TransformComponent>(player.AttachedEntity.Value).Coordinates
                : EntitySystem.Get<GameTicker>().GetObserverSpawnPoint();
            var brief = _entities.SpawnEntity("HumanoidSpawnerCentcomOfficial", coordinates);
            _entities.GetComponent<TransformComponent>(brief).AttachToGridOrMap();


            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
                _entities.GetComponent<MetaDataComponent>(brief).EntityName = mind.CharacterName;
            else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                _entities.GetComponent<MetaDataComponent>(brief).EntityName = mind.Session.Name;

            mind.Visit(brief);
        }
    }
}
