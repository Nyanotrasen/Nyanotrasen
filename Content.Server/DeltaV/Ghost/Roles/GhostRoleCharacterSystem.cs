using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.DeltaV.Ghost.Roles
{
    [UsedImplicitly]
    public sealed class GhostRoleCharacterSystem : EntitySystem
    {
        [Dependency] private readonly TransformSystem _transform = default!;
        [Dependency] private readonly IServerPreferencesManager _prefs = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GhostRoleCharacterSpawnerComponent, TakeGhostRoleEvent>(OnSpawnerTakeCharacter);
        }
        private void OnSpawnerTakeCharacter( EntityUid uid, GhostRoleCharacterSpawnerComponent component,
            ref TakeGhostRoleEvent args)
        {
            if (!TryComp(uid, out GhostRoleComponent? ghostRole) ||
                ghostRole.Taken)
            {
                args.TookRole = false;
                return;
            }

            var character = (HumanoidCharacterProfile) _prefs.GetPreferences(args.Player.UserId).SelectedCharacter;

            var mob = _entityManager.System<StationSpawningSystem>()
                .SpawnPlayerMob(Transform(uid).Coordinates, null, character, null);
            _transform.AttachToGridOrMap(mob);

            var spawnedEvent = new GhostRoleSpawnerUsedEvent(uid, mob);
            RaiseLocalEvent(mob, spawnedEvent);

            mob.EnsureComponent<MindComponent>();

            _entityManager.System<GhostRoleSystem>().GhostRoleInternalCreateMindAndTransfer(args.Player, uid, mob, ghostRole);

            if (++component.CurrentTakeovers < component.AvailableTakeovers)
            {
                args.TookRole = true;
                return;
            }

            ghostRole.Taken = true;

            if (component.DeleteOnSpawn)
                QueueDel(uid);

            args.TookRole = true;
        }
    }
}
