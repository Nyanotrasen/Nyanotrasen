using Content.Server.Ghost.Roles.Events;

namespace Content.Server.Fugitive
{
    public sealed class FugitiveSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FugitiveComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        }

        private void OnSpawned(EntityUid uid, FugitiveComponent component, GhostRoleSpawnerUsedEvent args)
        {
            Logger.Error("Fugi spawned!!!!");
        }
    }
}
