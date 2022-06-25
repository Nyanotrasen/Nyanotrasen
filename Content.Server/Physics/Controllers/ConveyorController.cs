using Content.Server.Conveyor;
using Content.Shared.Conveyor;
using Content.Shared.Movement.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;

namespace Content.Server.Physics.Controllers
{
    public sealed class ConveyorController : VirtualController
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ConveyorSystem _conveyor = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(MoverController));

            base.Initialize();
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            var conveyed = new HashSet<EntityUid>();

            // TODO: This won't work if someone wants a massive fuckoff conveyor so look at using StartCollide or something.
            foreach (var (comp, xform) in EntityManager.EntityQuery<ConveyorComponent, TransformComponent>())
            {
                Convey(comp, xform, conveyed, frameTime);
            }
        }

        private void Convey(ConveyorComponent comp, TransformComponent xform, HashSet<EntityUid> conveyed, float frameTime)
        {
            // Use an event for conveyors to know what needs to run
            if (!_conveyor.CanRun(comp))
            {
                return;
            }

            var speed = comp.Speed;

            if (speed <= 0f) return;

            var (conveyorPos, conveyorRot) = xform.GetWorldPositionRotation();

            conveyorRot += comp.Angle;

            if (comp.State == ConveyorState.Reverse)
            {
                conveyorRot += MathF.PI;
            }

            var direction = conveyorRot.ToWorldVec();

            foreach (var (entity, transform) in GetEntitiesToMove(comp, xform))
            {
                if (!conveyed.Add(entity)) continue;

                var worldPos = transform.WorldPosition;
                var itemRelative = conveyorPos - worldPos;

                worldPos += Convey(direction, speed, frameTime, itemRelative);
                transform.WorldPosition = worldPos;

                if (TryComp<PhysicsComponent>(entity, out var body))
                    body.Awake = true;
            }
        }

        private static Vector2 Convey(Vector2 direction, float speed, float frameTime, Vector2 itemRelative)
        {
            if (speed == 0 || direction.Length == 0) return Vector2.Zero;

            /*
             * Basic idea: if the item is not in the middle of the conveyor in the direction that the conveyor is running,
             * move the item towards the middle. Otherwise, move the item along the direction. This lets conveyors pick up
             * items that are not perfectly aligned in the middle, and also makes corner cuts work.
             *
             * We do this by computing the projection of 'itemRelative' on 'direction', yielding a vector 'p' in the direction
             * of 'direction'. We also compute the rejection 'r'. If the magnitude of 'r' is not (near) zero, then the item
             * is not on the centerline.
             */

            var p = direction * (Vector2.Dot(itemRelative, direction) / Vector2.Dot(direction, direction));
            var r = itemRelative - p;

            if (r.Length < 0.1)
            {
                var velocity = direction * speed;
                return velocity * frameTime;
            }
            else
            {
                var velocity = r.Normalized * speed;
                return velocity * frameTime;
            }
        }

        public IEnumerable<(EntityUid, TransformComponent)> GetEntitiesToMove(ConveyorComponent comp, TransformComponent xform)
        {
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid) ||
                !grid.TryGetTileRef(xform.Coordinates, out var tile)) yield break;

            var tileAABB = _lookup.GetLocalBounds(tile, grid.TileSize).Enlarged(0.01f);
            var gridMatrix = Transform(grid.GridEntityId).InvWorldMatrix;

            foreach (var entity in _lookup.GetEntitiesIntersecting(tile))
            {
                if (entity == comp.Owner ||
                    Deleted(entity) ||
                    HasComp<IMapGridComponent>(entity)) continue;

                if (!TryComp(entity, out PhysicsComponent? physics) ||
                    physics.BodyType == BodyType.Static ||
                    physics.BodyStatus == BodyStatus.InAir ||
                    entity.IsWeightless(physics, entityManager: EntityManager))
                {
                    continue;
                }

                if (_container.IsEntityInContainer(entity))
                {
                    continue;
                }

                // Yes there's still going to be the occasional rounding issue where it stops getting conveyed
                // When you fix the corner issue that will fix this anyway.
                var transform = Transform(entity);
                var gridPos = gridMatrix.Transform(transform.WorldPosition);
                var gridAABB = new Box2(gridPos - 0.1f, gridPos + 0.1f);

                if (!tileAABB.Intersects(gridAABB)) continue;

                yield return (entity, transform);
            }
        }
    }
}
