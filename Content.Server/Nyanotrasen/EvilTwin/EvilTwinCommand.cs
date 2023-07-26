using Content.Server.Administration;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.EvilTwin;

[AdminCommand(AdminFlags.Fun)]
public sealed class EvilTwinCommand : IConsoleCommand
{
    public string Command => "eviltwin";
    public string Description => Loc.GetString("command-eviltwin-description");
    public string Help => Loc.GetString("command-eviltwin-help");

    private const string _spawnerPrototype = "MobEvilTwinSpawn";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var evilTwinSystem = entityManager.EntitySysManager.GetEntitySystem<EvilTwinSystem>();

        EntityUid? specific = null;
        if (args.Length > 0 && EntityUid.TryParse(args[0], out var target))
            specific = target;

        if (!evilTwinSystem.TrySpawnParadoxAnomaly(out var twin, specific))
        {
            shell.WriteError("Unable to spawn a paradox anomaly.");
            return;
        }

        shell.WriteLine($"Spawned a paradox anomaly: {entityManager.ToPrettyString(twin.Value)}");

        var ghostRole = entityManager.EnsureComponent<GhostRoleComponent>(twin.Value);

        if (prototypeManager.TryIndex<EntityPrototype>(_spawnerPrototype, out var evilTwinSpawner) &&
            evilTwinSpawner.TryGetComponent<GhostRoleComponent>(out var spawnerRole))
        {
            ghostRole.RoleName = spawnerRole.RoleName;
            ghostRole.RoleDescription = spawnerRole.RoleDescription;
            ghostRole.RoleRules = spawnerRole.RoleRules;
        }

        entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(twin.Value);
    }
}
