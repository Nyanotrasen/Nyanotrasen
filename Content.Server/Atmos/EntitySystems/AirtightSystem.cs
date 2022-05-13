using Content.Server.Atmos.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Atmos;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class AirtightSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<AirtightComponent, ComponentInit>(OnAirtightInit);
            SubscribeLocalEvent<AirtightComponent, ComponentShutdown>(OnAirtightShutdown);
            SubscribeLocalEvent<AirtightComponent, AnchorStateChangedEvent>(OnAirtightPositionChanged);
            SubscribeLocalEvent<AirtightComponent, ReAnchorEvent>(OnAirtightReAnchor);
            SubscribeLocalEvent<AirtightComponent, RotateEvent>(OnAirtightRotated);
        }

        private void OnAirtightInit(EntityUid uid, AirtightComponent airtight, ComponentInit args)
        {
            var xform = EntityManager.GetComponent<TransformComponent>(uid);

            if (airtight.FixAirBlockedDirectionInitialize)
            {
                var rotateEvent = new RotateEvent(airtight.Owner, Angle.Zero, xform.LocalRotation, xform);
                OnAirtightRotated(uid, airtight, ref rotateEvent);
            }

            // Adding this component will immediately anchor the entity, because the atmos system
            // requires airtight entities to be anchored for performance.
            xform.Anchored = true;

            UpdatePosition(airtight);
        }

        private void OnAirtightShutdown(EntityUid uid, AirtightComponent airtight, ComponentShutdown args)
        {
            var xform = Transform(uid);

            // If the grid is deleting no point updating atmos.
            if (_mapManager.TryGetGrid(xform.GridID, out var grid))
            {
                if (MetaData(grid.GridEntityId).EntityLifeStage > EntityLifeStage.MapInitialized) return;
            }

            SetAirblocked(airtight, false, xform);
        }

        private void OnAirtightPositionChanged(EntityUid uid, AirtightComponent airtight, ref AnchorStateChangedEvent args)
        {
            var xform = Transform(uid);

            var gridId = xform.GridID;
            var coords = xform.Coordinates;

            var grid = _mapManager.GetGrid(gridId);
            var tilePos = grid.TileIndicesFor(coords);

            // Update and invalidate new position.
            airtight.LastPosition = (gridId, tilePos);
            InvalidatePosition(gridId, tilePos);
        }

        private void OnAirtightReAnchor(EntityUid uid, AirtightComponent airtight, ref ReAnchorEvent args)
        {
            foreach (var gridId in new[] { args.OldGrid, args.GridId })
            {
                // Update and invalidate new position.
                airtight.LastPosition = (gridId, args.TilePos);
                InvalidatePosition(gridId, args.TilePos);
            }
        }

        private void OnAirtightRotated(EntityUid uid, AirtightComponent airtight, ref RotateEvent ev)
        {
            if (!airtight.RotateAirBlocked || airtight.InitialAirBlockedDirection == (int)AtmosDirection.Invalid)
                return;

            airtight.CurrentAirBlockedDirection = (int) Rotate((AtmosDirection)airtight.InitialAirBlockedDirection, ev.NewRotation);
            UpdatePosition(airtight);
            RaiseLocalEvent(uid, new AirtightChanged(airtight));
        }

        public void SetAirblocked(AirtightComponent airtight, bool airblocked, TransformComponent? xform = null)
        {
            if (!Resolve(airtight.Owner, ref xform)) return;

            airtight.AirBlocked = airblocked;
            UpdatePosition(airtight, xform);
            RaiseLocalEvent(airtight.Owner, new AirtightChanged(airtight));
        }

        public void UpdatePosition(AirtightComponent airtight, TransformComponent? xform = null)
        {
            if (!Resolve(airtight.Owner, ref xform)) return;

            if (!xform.Anchored || !xform.GridID.IsValid())
                return;

            var grid = _mapManager.GetGrid(xform.GridID);
            airtight.LastPosition = (xform.GridID, grid.TileIndicesFor(xform.Coordinates));
            InvalidatePosition(airtight.LastPosition.Item1, airtight.LastPosition.Item2, airtight.FixVacuum && !airtight.AirBlocked);
        }

        public void InvalidatePosition(GridId gridId, Vector2i pos, bool fixVacuum = false)
        {
            if (!gridId.IsValid())
                return;

            var query = EntityManager.GetEntityQuery<AirtightComponent>();
            _explosionSystem.UpdateAirtightMap(gridId, pos, query);
            // TODO make atmos system use query
            _atmosphereSystem.UpdateAdjacent(gridId, pos);
            _atmosphereSystem.InvalidateTile(gridId, pos);

            if(fixVacuum)
                _atmosphereSystem.FixVacuum(gridId, pos);
        }

        private AtmosDirection Rotate(AtmosDirection myDirection, Angle myAngle)
        {
            var newAirBlockedDirs = AtmosDirection.Invalid;

            if (myAngle == Angle.Zero)
                return myDirection;

            // TODO ATMOS MULTIZ: When we make multiZ atmos, special case this.
            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);
                if (!myDirection.IsFlagSet(direction)) continue;
                var angle = direction.ToAngle();
                angle += myAngle;
                newAirBlockedDirs |= angle.ToAtmosDirectionCardinal();
            }

            return newAirBlockedDirs;
        }
    }

    public sealed class AirtightChanged : EntityEventArgs
    {
        public AirtightComponent Airtight;

        public AirtightChanged(AirtightComponent airtight)
        {
            Airtight = airtight;
        }
    }
}
