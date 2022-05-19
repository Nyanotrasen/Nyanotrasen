using Content.Server.Hands.Systems;
using Content.Server.Hands.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.Carrying
{
    public sealed class CarryingSystem : EntitySystem
    {
        [Dependency] private readonly HandVirtualItemSystem _virtualItemSystem = default!;

        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
            SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
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
                    Carry(args.User, uid);
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

        private void Carry(EntityUid carrier, EntityUid carried)
        {
            Transform(carried).Coordinates = Transform(carrier).Coordinates;
            Transform(carried).ParentUid = carrier;
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            EnsureComp<CarryingComponent>(carrier);
        }

        private void DropCarried(EntityUid carrier, EntityUid carried)
        {
            RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recusrively fire that event
            _virtualItemSystem.DeleteInHandsMatching(carrier, carried);
            Transform(carried).AttachToGridOrMap();
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
    }
}
