using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Hands.Components;
using Content.Server.MobState;
using Content.Server.Resist;
using Content.Server.Speech;
using Content.Server.Popups;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Stunnable;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Carrying;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.ActionBlocker;
using Content.Shared.Throwing;
using Content.Shared.Physics.Pull;
using Robust.Shared.Player;


namespace Content.Server.Carrying
{
    public sealed class CarryingSystem : EntitySystem
    {
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly CarryingSlowdownSystem _slowdown = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly StandingStateSystem _standingState = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedPullingSystem _pullingSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly EscapeInventorySystem _escapeInventorySystem = default!;
        [Dependency] private readonly VocalSystem _vocalSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
            SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnThrow);
            SubscribeLocalEvent<BeingCarriedComponent, MoveInputEvent>(OnMoveInput);
            SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, GettingInteractedWithAttemptEvent>(OnInteractedWith);
            SubscribeLocalEvent<BeingCarriedComponent, PullAttemptEvent>(OnPullAttempt);
            SubscribeLocalEvent<CarrySuccessfulEvent>(OnCarrySuccess);
            SubscribeLocalEvent<CarryCancelledEvent>(OnCarryCancelled);
        }


        private void AddCarryVerb(EntityUid uid, CarriableComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!CanCarry(args.User, uid, component))
                return;

            if (HasComp<CarryingComponent>(args.User)) // yeah not dealing with that
                return;

            if (HasComp<BeingCarriedComponent>(args.User) || HasComp<BeingCarriedComponent>(args.Target))
                return;

            if (!_mobStateSystem.IsAlive(args.User))
                return;

            if (args.User == args.Target)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartCarryDoAfter(args.User, uid, component);
                },
                Text = Loc.GetString("carry-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnVirtualItemDeleted(EntityUid uid, CarryingComponent component, VirtualItemDeletedEvent args)
        {
            if (!HasComp<CarriableComponent>(args.BlockingEntity))
                return;

            DropCarried(uid, args.BlockingEntity);
        }

        private void OnThrow(EntityUid uid, CarryingComponent component, BeforeThrowEvent args)
        {
            if (!TryComp<HandVirtualItemComponent>(args.ItemUid, out var virtItem) || !HasComp<CarriableComponent>(virtItem.BlockingEntity))
                return;

            args.ItemUid = virtItem.BlockingEntity;

            if (!TryComp<PhysicsComponent>(args.ItemUid, out var itemPhysics) || !TryComp<PhysicsComponent>(args.PlayerUid, out var playerPhysics))
                return;

            var multiplier = (playerPhysics.FixturesMass / itemPhysics.FixturesMass);
            args.ThrowStrength = 5f * multiplier;

            _vocalSystem.TryScream(args.ItemUid);
        }

        private void OnMoveInput(EntityUid uid, BeingCarriedComponent component, ref MoveInputEvent args)
        {
            if (!TryComp<CanEscapeInventoryComponent>(uid, out var escape) || escape.IsResisting)
                return;

            if (_actionBlockerSystem.CanInteract(uid, component.Carrier))
                _escapeInventorySystem.AttemptEscape(uid, component.Carrier, escape);
        }
        private void OnMoveAttempt(EntityUid uid, BeingCarriedComponent component, UpdateCanMoveEvent args)
        {
            args.Cancel();
        }

        private void OnStandAttempt(EntityUid uid, BeingCarriedComponent component, StandAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnInteractedWith(EntityUid uid, BeingCarriedComponent component, GettingInteractedWithAttemptEvent args)
        {
            if (args.Uid != component.Carrier)
                args.Cancel();
        }

        private void OnPullAttempt(EntityUid uid, BeingCarriedComponent component, PullAttemptEvent args)
        {
            args.Cancelled = true;
        }

        private void OnCarrySuccess(CarrySuccessfulEvent ev)
        {
            if (!CanCarry(ev.Carrier, ev.Carried, ev.Component))
                return;

            Carry(ev.Carrier, ev.Carried);
        }

        private void OnCarryCancelled(CarryCancelledEvent ev)
        {
            if (ev.Component == null)
                return;

            ev.Component.CancelToken = null;
        }

        private void StartCarryDoAfter(EntityUid carrier, EntityUid carried, CarriableComponent component)
        {
            if (component.CancelToken != null)
            {
                component.CancelToken.Cancel();
                component.CancelToken = null;
            }

            float length = 3f;

            if (TryComp<PhysicsComponent>(carrier, out var carrierPhysics) && TryComp<PhysicsComponent>(carried, out var carriedPhysics))
            {
                length /= (carrierPhysics.FixturesMass / carriedPhysics.FixturesMass);
                if (length >= 9)
                {
                    _popupSystem.PopupEntity(Loc.GetString("carry-too-heavy"), carried, Filter.Entities(carrier), Shared.Popups.PopupType.SmallCaution);
                    return;
                }
            } else
            {
                return;
            }

            if (!HasComp<KnockedDownComponent>(carried))
                length *= 2f;

            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(carrier, length, component.CancelToken.Token, target: carried)
            {
                BroadcastFinishedEvent = new CarrySuccessfulEvent(carrier, carried, component),
                BroadcastCancelledEvent = new CarryCancelledEvent(carrier, component),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        private void Carry(EntityUid carrier, EntityUid carried)
        {
            if (TryComp<SharedPullableComponent>(carried, out var pullable))
                _pullingSystem.TryStopPull(pullable);

            Transform(carried).Coordinates = Transform(carrier).Coordinates;
            Transform(carried).ParentUid = carrier;
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            EnsureComp<CarryingComponent>(carrier);
            ApplyCarrySlowdown(carrier, carried);
            var carriedComp = EnsureComp<BeingCarriedComponent>(carried);
            EnsureComp<KnockedDownComponent>(carried);
            carriedComp.Carrier = carrier;
            _actionBlockerSystem.UpdateCanMove(carried);
        }

        public void DropCarried(EntityUid carrier, EntityUid carried)
        {
            RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recusrively fire that event
            RemComp<CarryingSlowdownComponent>(carrier);
            RemComp<BeingCarriedComponent>(carried);
            RemComp<KnockedDownComponent>(carried);
            _actionBlockerSystem.UpdateCanMove(carried);
            _virtualItemSystem.DeleteInHandsMatching(carrier, carried);
            Transform(carried).AttachToGridOrMap();
            _standingState.Stand(carried);
            _movementSpeed.RefreshMovementSpeedModifiers(carrier);
        }

        private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
        {
            if (!TryComp<PhysicsComponent>(carrier, out var carrierPhysics))
                return;
            if (!TryComp<PhysicsComponent>(carried, out var carriedPhysics))
                return;

            var carrierMass = carrierPhysics.FixturesMass;
            var carriedMass = carriedPhysics.FixturesMass;

            if (carrierMass == 0f)
                carrierMass = 70f;
            if (carriedMass == 0f)
                carriedMass = 70f;

            var massRatioSq = Math.Pow((carriedMass / carrierMass), 2);
            var modifier = (1 - (massRatioSq * 0.15));
            var slowdownComp = EnsureComp<CarryingSlowdownComponent>(carrier);
            _slowdown.SetModifier(carrier, (float) modifier, (float) modifier, slowdownComp);
        }

        public bool CanCarry(EntityUid carrier, EntityUid carried, CarriableComponent? carriedComp = null)
        {
            if (!Resolve(carried, ref carriedComp))
                return false;

            if (!TryComp<HandsComponent>(carrier, out var hands))
                return false;

            if (hands.CountFreeHands() < carriedComp.FreeHandsRequired)
                return false;

            return true;
        }

        private sealed class CarryCancelledEvent : EntityEventArgs
        {
            public EntityUid Uid;

            public CarriableComponent Component;

            public CarryCancelledEvent(EntityUid uid, CarriableComponent component)
            {
                Uid = uid;
                Component = component;
            }
        }

        private sealed class CarrySuccessfulEvent : EntityEventArgs
        {
            public EntityUid Carrier;

            public EntityUid Carried;

            public CarriableComponent Component;

            public CarrySuccessfulEvent(EntityUid carrier, EntityUid carried, CarriableComponent component)
            {
                Carrier = carrier;
                Carried = carried;
                Component = component;
            }
        }
    }
}
