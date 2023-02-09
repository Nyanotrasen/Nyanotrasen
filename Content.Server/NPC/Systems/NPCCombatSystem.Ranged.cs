using Content.Server.NPC.Components;
using Content.Server.NPC.Events;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using static Content.Shared.Examine.ExamineSystemShared;
using Content.Shared.NPC;

namespace Content.Server.NPC.Systems;

public sealed partial class NPCCombatSystem
{
    [Dependency] private readonly RotateToFaceSystem _rotate = default!;

    // TODO: Don't predict for hitscan
    private const float ShootSpeed = 20f;

    /// <summary>
    /// Cooldown on raycasting to check LOS.
    /// </summary>
    public const float UnoccludedCooldown = 0.2f;

    private void InitializeRanged()
    {
        SubscribeLocalEvent<NPCRangedCombatComponent, NPCSteeringEvent>(OnRangedSteering);
        SubscribeLocalEvent<NPCRangedCombatComponent, ComponentStartup>(OnRangedStartup);
        SubscribeLocalEvent<NPCRangedCombatComponent, ComponentShutdown>(OnRangedShutdown);
    }

    private void OnRangedSteering(EntityUid uid, NPCRangedCombatComponent component, ref NPCSteeringEvent args)
    {
        args.Steering.CanSeek = true;

        if (!_physics.TryGetNearestPoints(uid, component.Target, out _, out var pointB))
            return;

        var idealDistance = 4f;
        var obstacleDirection = pointB - args.WorldPosition;
        var obstacleDistance = obstacleDirection.Length;

        if (obstacleDistance > idealDistance || obstacleDistance < 1f)
        {
            return;
        }

        args.Steering.CanSeek = false;
        obstacleDirection = args.OffsetRotation.RotateVec(obstacleDirection);
        var norm = obstacleDirection.Normalized;

        var weight = (idealDistance - obstacleDistance) / idealDistance;

        for (var i = 0; i < SharedNPCSteeringSystem.InterestDirections; i++)
        {
            var result = -Vector2.Dot(norm, NPCSteeringSystem.Directions[i]) * weight;

            if (result < 0f)
                continue;

            args.Interest[i] = MathF.Max(args.Interest[i], result);
        }
    }


    private void OnRangedStartup(EntityUid uid, NPCRangedCombatComponent component, ComponentStartup args)
    {
        if (TryComp<SharedCombatModeComponent>(uid, out var combat))
        {
            combat.IsInCombatMode = true;
        }
        else
        {
            component.Status = CombatStatus.Unspecified;
        }
    }

    private void OnRangedShutdown(EntityUid uid, NPCRangedCombatComponent component, ComponentShutdown args)
    {
        if (TryComp<SharedCombatModeComponent>(uid, out var combat))
        {
            combat.IsInCombatMode = false;
        }

        _steering.Unregister(component.Owner);
    }

    private void UpdateRanged(float frameTime)
    {
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var combatQuery = GetEntityQuery<SharedCombatModeComponent>();

        foreach (var (comp, xform) in EntityQuery<NPCRangedCombatComponent, TransformComponent>())
        {
            if (comp.Status == CombatStatus.Unspecified)
                continue;

            if (!xformQuery.TryGetComponent(comp.Target, out var targetXform) ||
                !bodyQuery.TryGetComponent(comp.Target, out var targetBody))
            {
                comp.Status = CombatStatus.TargetUnreachable;
                comp.ShootAccumulator = 0f;
                continue;
            }

            if (targetXform.MapID != xform.MapID)
            {
                comp.Status = CombatStatus.TargetUnreachable;
                comp.ShootAccumulator = 0f;
                continue;
            }

            if (combatQuery.TryGetComponent(comp.Owner, out var combatMode))
            {
                combatMode.IsInCombatMode = true;
            }

            var gun = _gun.GetGun(comp.Owner);

            if (gun == null)
            {
                comp.Status = CombatStatus.NoWeapon;
                comp.ShootAccumulator = 0f;
                continue;
            }

            comp.LOSAccumulator -= frameTime;

            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform, xformQuery);

            // We'll work out the projected spot of the target and shoot there instead of where they are.
            var distance = (targetPos - worldPos).Length;
            var oldInLos = comp.TargetInLOS;

            // TODO: Should be doing these raycasts in parallel
            // Ideally we'd have 2 steps, 1. to go over the normal details for shooting and then 2. to handle beep / rotate / shoot
            if (comp.LOSAccumulator < 0f)
            {
                comp.LOSAccumulator += UnoccludedCooldown;
                comp.TargetInLOS = ExamineSystemShared.InRangeUnOccluded(comp.Owner, comp.Target, distance + 0.1f, null);
            }

            if (!comp.TargetInLOS)
            {
                comp.ShootAccumulator = 0f;
                if (!comp.CanMove || distance >= 9f)
                {
                    comp.Status = CombatStatus.TargetUnreachable;
                    continue;
                }
                else
                {
                    comp.Status = CombatStatus.NotInSight;
                }
            }

            if (!oldInLos && comp.SoundTargetInLOS != null)
            {
                _audio.PlayPvs(comp.SoundTargetInLOS, comp.Owner);
            }

            comp.ShootAccumulator += frameTime;

            if (comp.ShootAccumulator < comp.ShootDelay)
            {
                continue;
            }

            var mapVelocity = targetBody.LinearVelocity;
            var targetSpot = targetPos + mapVelocity * distance / ShootSpeed;

            // If we have a max rotation speed then do that.
            var goalRotation = (targetSpot - worldPos).ToWorldAngle();
            var rotationSpeed = comp.RotationSpeed;

            if (!_rotate.TryRotateTo(comp.Owner, goalRotation, frameTime, comp.AccuracyThreshold, rotationSpeed?.Theta ?? double.MaxValue, xform))
            {
                continue;
            }


            if (comp.CanMove)
            {
                if (TryComp<NPCSteeringComponent>(comp.Owner, out var steering) &&
                    steering.Status == SteeringStatus.NoPath)
                {
                    comp.Status = CombatStatus.TargetUnreachable;
                    return;
                }

                steering = EnsureComp<NPCSteeringComponent>(comp.Owner);
                steering.Range = 3.5f;

                // Gets unregistered on component shutdown.
                _steering.TryRegister(comp.Owner, new EntityCoordinates(comp.Target, Vector2.Zero), steering);
            }

            // TODO: LOS
            // TODO: Ammo checks
            // TODO: Burst fire
            // TODO: Cycling
            // Max rotation speed

            // TODO: Check if we can face

            if (!Enabled || !_gun.CanShoot(gun))
                continue;

            EntityCoordinates targetCordinates;

            if (_mapManager.TryFindGridAt(xform.MapID, targetPos, out var mapGrid))
            {
                targetCordinates = new EntityCoordinates(mapGrid.Owner, mapGrid.WorldToLocal(targetSpot));
            }
            else
            {
                targetCordinates = new EntityCoordinates(xform.MapUid!.Value, targetSpot);
            }

            _gun.AttemptShoot(comp.Owner, gun, targetCordinates);
        }
    }
}
