using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Hands.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Stunnable;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Carrying;
using Content.Shared.Movement;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.ActionBlocker;
using Robust.Shared.Physics;

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
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
            SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, GettingInteractedWithAttemptEvent>(OnInteractedWith);
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

            if (!HasComp<KnockedDownComponent>(uid) && !(TryComp<MobStateComponent>(uid, out var state) && (state.IsCritical() || state.IsDead() || state.IsIncapacitated())))
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

            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(carrier, 3f, component.CancelToken.Token, target: carried)
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
            carriedComp.Carrier = carrier;
            _actionBlockerSystem.UpdateCanMove(carried);
        }

        public void DropCarried(EntityUid carrier, EntityUid carried)
        {
            RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recusrively fire that event
            RemComp<CarryingSlowdownComponent>(carrier);
            RemComp<BeingCarriedComponent>(carried);
            _actionBlockerSystem.UpdateCanMove(carried);
            _virtualItemSystem.DeleteInHandsMatching(carrier, carried);
            Transform(carried).AttachToGridOrMap();
            _standingState.Stand(carried);
        }

        private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
        {
            if (!TryComp<FixturesComponent>(carrier, out var carrierFixtures))
                return;
            if (!TryComp<FixturesComponent>(carried, out var carriedFixtures))
                return;
            if (carrierFixtures.Fixtures.Count == 0 || carriedFixtures.Fixtures.Count == 0)
                return;

            float carrierMass = 0f;
            float carriedMass = 0f;
            foreach (var fixture in carrierFixtures.Fixtures.Values)
            {
                carrierMass += fixture.Mass;
            }
            foreach (var fixture in carriedFixtures.Fixtures.Values)
            {
                carriedMass += fixture.Mass;
            }

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
