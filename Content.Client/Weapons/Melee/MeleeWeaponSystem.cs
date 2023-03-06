using Content.Client.CombatMode;
using Content.Client.Gameplay;
using Content.Client.Hands;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.StatusEffect;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    private const string MeleeLungeKey = "melee-lunge";

    public override void Initialize()
    {
        base.Initialize();
        InitializeEffect();
        SubscribeAllEvent<DamageEffectEvent>(OnDamageEffect);
        SubscribeNetworkEvent<MeleeLungeEvent>(OnMeleeLunge);
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalPlayer?.ControlledEntity;

        if (entityNull == null)
            return;

        var entity = entityNull.Value;
        var weapon = GetWeapon(entity);

        if (weapon == null)
            return;

        if (!CombatMode.IsInCombatMode(entity) || !Blocker.CanAttack(entity))
        {
            weapon.Attacking = false;
            return;
        }

        // TODO using targeted actions while combat mode is enabled should NOT trigger attacks.

        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);
        var currentTime = Timing.CurTime;

        // Heavy attack.
        if (altDown == BoundKeyState.Down)
        {
            // We did the click to end the attack but haven't pulled the key up.
            if (weapon.Attacking)
            {
                return;
            }

            var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            EntityCoordinates coordinates;

            if (MapManager.TryFindGridAt(mousePos, out var grid))
            {
                coordinates = EntityCoordinates.FromMap(grid.Owner, mousePos, EntityManager);
            }
            else
            {
                coordinates = EntityCoordinates.FromMap(MapManager.GetMapEntityId(mousePos.MapId), mousePos, EntityManager);
            }

            // If it's an unarmed attack then do a disarm
            if (weapon.Owner == entity)
            {
                EntityUid? target = null;

                if (_stateManager.CurrentState is GameplayStateBase screen)
                {
                    target = screen.GetClickedEntity(mousePos);
                }

                EntityManager.RaisePredictiveEvent(new DisarmAttackEvent(target, coordinates));
                return;
            }

            // Try to do a heavy attack.
            EntityManager.RaisePredictiveEvent(new HeavyAttackEvent(weapon.Owner, coordinates));
            return;
        }

        // Light attack
        if (useDown == BoundKeyState.Down)
        {
            if (weapon.Attacking || weapon.NextAttack > Timing.CurTime)
            {
                return;
            }

            var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            var attackerPos = Transform(entity).MapPosition;

            if (mousePos.MapId != attackerPos.MapId ||
                (attackerPos.Position - mousePos.Position).Length > weapon.Range)
            {
                return;
            }

            EntityCoordinates coordinates;

            // Bro why would I want a ternary here
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (MapManager.TryFindGridAt(mousePos, out var grid))
            {
                coordinates = EntityCoordinates.FromMap(grid.Owner, mousePos, EntityManager);
            }
            else
            {
                coordinates = EntityCoordinates.FromMap(MapManager.GetMapEntityId(mousePos.MapId), mousePos, EntityManager);
            }

            EntityUid? target = null;

            // TODO: UI Refactor update I assume
            if (_stateManager.CurrentState is GameplayStateBase screen)
            {
                target = screen.GetClickedEntity(mousePos);
            }

            RaisePredictiveEvent(new LightAttackEvent(target, weapon.Owner, coordinates));
            return;
        }

        if (weapon.Attacking)
        {
            RaisePredictiveEvent(new StopAttackEvent(weapon.Owner));
        }
    }

    protected override bool InRange(EntityUid user, EntityUid target, float range, ICommonSession? session)
    {
        var xform = Transform(target);
        var targetCoordinates = xform.Coordinates;
        var targetLocalAngle = xform.LocalRotation;

        return Interaction.InRangeUnobstructed(user, target, targetCoordinates, targetLocalAngle, range);
    }

    protected override void DoDamageEffect(List<EntityUid> targets, EntityUid? user, TransformComponent targetXform)
    {
        // Server never sends the event to us for predictiveeevent.
        if (_timing.IsFirstTimePredicted)
            RaiseLocalEvent(new DamageEffectEvent(Color.Red, targets));
    }

    protected override bool DoDisarm(EntityUid user, DisarmAttackEvent ev, MeleeWeaponComponent component, ICommonSession? session)
    {
        if (!base.DoDisarm(user, ev, component, session))
            return false;

        if (!TryComp<CombatModeComponent>(user, out var combatMode) ||
            combatMode.CanDisarm != true)
        {
            return false;
        }

        // They need to either have hands...
        if (!HasComp<HandsComponent>(ev.Target!.Value))
        {
            // or just be able to be shoved over.
            if (TryComp<StatusEffectsComponent>(ev.Target!.Value, out var status) && status.AllowedEffects.Contains("KnockedDown"))
                return true;

            if (Timing.IsFirstTimePredicted && HasComp<MobStateComponent>(ev.Target.Value))
                PopupSystem.PopupEntity(Loc.GetString("disarm-action-disarmable", ("targetName", ev.Target.Value)), ev.Target.Value);

            return false;
        }

        return true;
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (!Timing.IsFirstTimePredicted || uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value);
    }

    private void OnMeleeLunge(MeleeLungeEvent ev)
    {
        // Entity might not have been sent by PVS.
        if (Exists(ev.Entity))
            DoLunge(ev.Entity, ev.Angle, ev.LocalPos, ev.Animation);
    }
}
