using Robust.Client.GameObjects;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Content.Client.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Coordinates;

namespace Content.Client.Mapping
{
    public sealed class MapCopyPlacementHijack : PlacementHijack
    {
        private readonly IEntityManager _entityManager;
        private readonly IMapManager _mapManager;
        private readonly IPlayerManager _playerManager;
        private readonly ActionsSystem _actionsSystem;
        private readonly EntityLookupSystem _entityLookupSystem;
        private readonly SharedTransformSystem _transformSystem;
        private readonly SpriteSystem _spriteSystem;

        private MapCopyBuffer _buffer = new();
        private EntityCoordinates _origin;

        private SpriteSpecifier _startCopyTexture = new SpriteSpecifier.Texture(new ResPath("Nyanotrasen/Interface/copy_reticle.png"));
        private SpriteSpecifier _endCopyTexture = new SpriteSpecifier.Texture(new ResPath("Nyanotrasen/Interface/copy_done_reticle.png"));
        private SpriteSpecifier _pasteTexture = new SpriteSpecifier.Texture(new ResPath("Nyanotrasen/Interface/paste_action.png"));

        public MapCopyPlacementHijack(IEntityManager entityManager,
            IMapManager mapManager,
            IPlayerManager playerManager,
            ActionsSystem actionsSystem,
            EntityLookupSystem entityLookupSystem,
            SharedTransformSystem transformSystem,
            SpriteSystem spriteSystem)
        {
            _entityManager = entityManager;
            _mapManager = mapManager;
            _playerManager = playerManager;
            _actionsSystem = actionsSystem;
            _entityLookupSystem = entityLookupSystem;
            _transformSystem = transformSystem;
            _spriteSystem = spriteSystem;
        }

        public override bool HijackPlacementRequest(EntityCoordinates coordinates)
        {
            if (_origin == EntityCoordinates.Invalid)
            {
                _origin = coordinates;
                Manager.CurrentTextures = new () { _spriteSystem.RsiStateLike(_endCopyTexture) };
                return true;
            }

            var gridId = coordinates.GetGridUid(_entityManager);
            if (gridId == null)
                return false;

            if (_actionsSystem.PlayerActions == null)
                return false;

            if (_playerManager.LocalPlayer?.ControlledEntity is not {Valid: true} playerEntity)
                return false;

            var grid = _mapManager.GetGrid(gridId.Value);

            // Anchor the buffer around the first tile picked.
            var start = grid.ToCoordinates(grid.CoordinatesToTile(_origin));
            var startTile = grid.GetTileRef(_origin);

            // Figure out all the relative positioning.
            var gridXform = _entityManager.GetComponent<TransformComponent>(gridId.Value);
            var xforms = _entityManager.GetEntityQuery<TransformComponent>();
            var (gridPos, gridRot) = _transformSystem.GetWorldPositionRotation(gridXform, xforms);

            var aabb = new Box2Rotated(
                Box2.FromTwoPoints(
                    _origin.Position,
                    coordinates.Position).Translated(gridPos),
                gridRot,
                gridPos);

            var buffer = new MapCopyBuffer();

            SpriteSpecifier? actionIconCandidate = null;

            // Copy the entities.
            foreach (var entity in _entityLookupSystem.GetEntitiesIntersecting(gridId.Value,
                    aabb,
                    LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries))
            {
                if (entity == playerEntity)
                    continue;

                var proto = _entityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype?.ID;
                if (proto == null)
                    continue;

                if (actionIconCandidate == null)
                    actionIconCandidate = new SpriteSpecifier.EntityPrototype(proto);

                var xform = _entityManager.GetComponent<TransformComponent>(entity);
                var entCoords = xform.Coordinates;
                var offset = (entCoords - start).Position;
                var rotation = xform.LocalRotation;

                buffer.Entities.Add((offset, rotation, proto));
            }

            // TODO: Copy the decals.
            // GetDecalsIntersecting is server-only at this point.

            // Copy the tiles.
            foreach (var tile in grid.GetTilesIntersecting(aabb))
            {
                var offset = tile.GridIndices - startTile.GridIndices;

                buffer.Tiles.Add((offset, tile.Tile.TypeId));
            }

            var actionTime = DateTime.Now;
            var actionUid = actionTime.Ticks;
            var actionEvent = new MapPasteBufferActionEvent() { Buffer = buffer };
            var action = new WorldTargetAction()
            {
                Range = 0,
                Temporary = true,
                ClientExclusive = true,
                CheckCanAccess = false,
                CheckCanInteract = false,
                Repeat = true,
                Event = actionEvent,
                // So the client can have multiple paste buffers:
                DisplayName = $"Paste (buffer {actionUid})",
                Description = $"Paste buffer created at {actionTime}",
                Icon = actionIconCandidate ?? _pasteTexture
            };

            _actionsSystem.AddAction(playerEntity, action, null);

            // Reset the copy origin.
            _origin = EntityCoordinates.Invalid;
            Manager.CurrentTextures = new () { _spriteSystem.RsiStateLike(_startCopyTexture) };

            return true;
        }

        public override void StartHijack(PlacementManager manager)
        {
            base.StartHijack(manager);

            manager.CurrentTextures = new () { _spriteSystem.RsiStateLike(_startCopyTexture) };
        }
    }
}

