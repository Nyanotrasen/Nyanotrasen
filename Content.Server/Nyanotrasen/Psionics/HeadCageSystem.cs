using System.Threading;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Abilities.Psionics;
using Content.Server.DoAfter;
using Robust.Shared.Audio;

namespace Content.Server.Psionics
{
    public sealed class HeadCageSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly AlertsSystem _alertsSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HeadCageComponent, GotEquippedEvent>(OnCageEquipped);
            SubscribeLocalEvent<HeadCageComponent, GotUnequippedEvent>(OnCageUnequipped);
            SubscribeLocalEvent<ResistCageSuccessfulEvent>(OnResistCageSuccessful);
            SubscribeLocalEvent<ResistCageCancelledEvent>(OnResistCageCancelled);
        }

        private void OnCageEquipped(EntityUid uid, HeadCageComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<SharedClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            component.IsActive = true;
            AddComp<UnremoveableComponent>(uid);
            _alertsSystem.ShowAlert(args.Equipee, AlertType.Caged);
        }

        private void OnCageUnequipped(EntityUid uid, HeadCageComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            component.IsActive = false;
        }

        private void OnResistCageCancelled(ResistCageCancelledEvent args)
        {
            if (!EntityManager.TryGetComponent<HeadCageComponent>(args.Cage, out var cageComp))
                return;

            cageComp.CancelToken = null;
        }

        private void OnResistCageSuccessful(ResistCageSuccessfulEvent args)
        {
            if (!EntityManager.TryGetComponent<HeadCageComponent>(args.Cage, out var cageComp))
                return;

            _audioSystem.PlayPvs(cageComp.EndCageSound, args.Caged);

            cageComp.CancelToken = null;

            RemComp<UnremoveableComponent>(args.Cage);
            if (_inventory.TryUnequip(args.Caged, "head", force: true))
                _alertsSystem.ClearAlert(args.Caged, AlertType.Caged);
        }

        public void ResistCage(EntityUid uid)
        {
            if (!_blocker.CanInteract(uid, uid))
                return;

            if (!_inventory.TryGetSlotEntity(uid, "head", out var headItem) || !TryComp<HeadCageComponent>(headItem, out var cageComp))
            {
                _alertsSystem.ClearAlert(uid, AlertType.Caged);
                return;
            }

            if (cageComp.CancelToken != null)
                return;

            _audioSystem.PlayPvs(cageComp.StartBreakoutSound, uid);

            cageComp.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, 5, cageComp.CancelToken.Token)
            {
                BroadcastFinishedEvent = new ResistCageSuccessfulEvent(uid, headItem.Value),
                BroadcastCancelledEvent = new ResistCageCancelledEvent(uid, headItem.Value),
                BreakOnUserMove = true,
                BreakOnStun = true,
                BreakOnDamage = true,
                NeedHand = true
            });
        }


        private sealed class ResistCageCancelledEvent : EntityEventArgs
        {
            public EntityUid Caged;
            public EntityUid Cage;
            public ResistCageCancelledEvent(EntityUid caged, EntityUid cage)
            {
                Caged = caged;
                Cage = cage;
            }
        }

        private sealed class ResistCageSuccessfulEvent : EntityEventArgs
        {
            public EntityUid Caged;
            public EntityUid Cage;
            public ResistCageSuccessfulEvent(EntityUid caged, EntityUid cage)
            {
                Caged = caged;
                Cage = cage;
            }
        }
    }
}
