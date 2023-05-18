using System.Linq;
using Content.Server.DetailExaminable;
using Content.Shared.CCVar;
using Content.Server.Players;
using Content.Server.Humanoid;
using Content.Shared.Administration;
using Content.Server.IdentityManagement;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid.Prototypes;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands.loadcharacter
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed class loadcharacter : IConsoleCommand
    {
        [Dependency] private readonly IEntitySystemManager _entitysys = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IServerPreferencesManager _prefs = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;

        public string Command => "loadcharacter";
        public string Description => "Applies your currently selected character to an entity";
        public string Help => $"Usage: {Command} | {Command} <entityUid> | {Command} <entityUid> <characterName>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            if (player == null)
            {
                shell.WriteError("You aren't a player.");
                return;
            }

            var mind = player.ContentData()?.Mind;

            if (mind == null || mind.UserId == null)
            {
                shell.WriteError("You don't have a mind.");
                return;
            }

            EntityUid target;

            if (args.Length >= 1)
            {
                if (!int.TryParse(args.First(), out var entityUid))
                {
                    shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }
                target = new EntityUid(entityUid);
            }
            else
            {
                if (player.AttachedEntity == null ||
                    !_entityManager.HasComponent<HumanoidAppearanceComponent>(player.AttachedEntity.Value))
                {
                    shell.WriteError(Loc.GetString("shell-target-entity-does-not-have-message",
                        ("missing", "AttachedEntity")));
                    return;
                }
                target = player.AttachedEntity.Value;
            }

            if (!target.IsValid() || !_entityManager.EntityExists(target))
            {
                shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
                return;
            }

            if (target == null ||
                !_entityManager.TryGetComponent<HumanoidAppearanceComponent>(target, out var humanoidAppearance))
            {
                shell.WriteError(Loc.GetString("shell-entity-with-uid-lacks-component", ("uid", target), ("componentName", nameof(HumanoidAppearanceComponent))));
                return;
            }

            HumanoidCharacterProfile character = new();

            if (args.Length >= 2)
            {
                // This seems like a bad way to go about it, but it works so eh?
                var name = String.Join(" ", args.Skip(1).ToArray());
                shell.WriteLine($"Attempting to fetch character '{name}'");

                var charIndex = _prefs.GetPreferences(mind.UserId.Value).IndexOfCharacterName(name);
                if (charIndex < 0)
                {
                    shell.WriteError("Fetching failed!");
                    return;
                }

                character = (HumanoidCharacterProfile) _prefs.GetPreferences(mind.UserId.Value).GetProfile(charIndex);
            }
            else
                character = (HumanoidCharacterProfile) _prefs.GetPreferences(mind.UserId.Value).SelectedCharacter;

            // This shouldn't ever fail considering the previous checks
            if (!_prototypeManager.TryIndex<SpeciesPrototype>(humanoidAppearance.Species, out var speciesPrototype) || !_prototypeManager.TryIndex<SpeciesPrototype>(character.Species, out var entPrototype))
                return;

            if (speciesPrototype != entPrototype)
                shell.WriteLine("Species mismatch detected between character and selected entity, this may have unexpected results.");

            _entityManager.System<HumanoidAppearanceSystem>().LoadProfile(target, character);

            if (_entityManager.TryGetComponent<MetaDataComponent>(target, out var metadata))
                metadata.EntityName = character.Name;

            if (character.FlavorText != "" && _configurationManager.GetCVar(CCVars.FlavorText))
            {
                _entityManager.EnsureComponent<DetailExaminableComponent>(target).Content = character.FlavorText;
            }
            else
                _entityManager.RemoveComponent<DetailExaminableComponent>(target);

            _entityManager.System<IdentitySystem>().QueueIdentityUpdate(target);
            shell.WriteLine("Character load complete");
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 2)
            {
                var player = shell.Player as IPlayerSession;
                if (player == null)
                    return CompletionResult.Empty;
                var mind = player.ContentData()?.Mind;
                if (mind == null || mind.UserId == null)
                    return CompletionResult.Empty;

                return CompletionResult.FromHintOptions(_prefs.GetPreferences(mind.UserId.Value).GetCharacterNames(), "Select Character");
            }

            return CompletionResult.Empty;
        }
    }
}
