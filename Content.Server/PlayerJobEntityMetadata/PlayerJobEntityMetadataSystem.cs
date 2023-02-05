using Content.Server.DetailExaminable;
using Content.Server.IdentityManagement;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.SiliconJobMetadata;

public sealed class PlayerJobEntityMetadataSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerJobEntityMetadataComponent, PlayerMobSpawnedEvent>(OnSpawnPlayer);
    }

    private void OnSpawnPlayer(EntityUid playerMob, PlayerJobEntityMetadataComponent component, PlayerMobSpawnedEvent args)
    {
        var meta = MetaData(playerMob);
        var profile = args.HumanoidCharacterProfile;

        if (profile != null)
        {
            meta.EntityName = profile.Name;
        }

        if (profile != null)
        {
            meta.EntityName = profile.Name;
            if (profile.FlavorText != "" && _configurationManager.GetCVar(CCVars.FlavorText))
            {
                EntityManager.AddComponent<DetailExaminableComponent>(playerMob).Content = profile.FlavorText;
            }
        }

        _identity.QueueIdentityUpdate(playerMob);
    }
}
