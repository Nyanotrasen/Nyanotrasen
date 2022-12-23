using System.Threading;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction;
using Content.Shared.IdentityManagement;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Verbs;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Robust.Shared.Player;

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
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HeadCageComponent, GotEquippedEvent>(OnCageEquipped);
            SubscribeLocalEvent<HeadCageComponent, GotUnequippedEvent>(OnCageUnequipped);
            SubscribeLocalEvent<HeadCageComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<HeadCagedComponent, GetVerbsEvent<AlternativeVerb>>(AddUncageVerb);
            SubscribeLocalEvent<ResistCageSuccessfulEvent>(OnResistCageSuccessful);
            SubscribeLocalEvent<ResistCageCancelledEvent>(OnResistCageCancelled);
            SubscribeLocalEvent<PutOnCageCancelledEvent>(OnPutOnCageCancelled);
            SubscribeLocalEvent<PutOnCageSuccessfulEvent>(OnPutOnCageSuccessful);
        }

        private void OnCageEquipped(EntityUid uid, HeadCageComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;

            component.IsActive = true;
            EnsureComp<HeadCagedComponent>(args.Equipee);
            AddComp<UnremoveableComponent>(uid);
            _alertsSystem.ShowAlert(args.Equipee, AlertType.Caged);
        }

        private void OnCageUnequipped(EntityUid uid, HeadCageComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            RemComp<HeadCagedComponent>(args.Equipee);
            component.IsActive = false;
        }

        private void OnAfterInteract(EntityUid uid, HeadCageComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (component.CancelToken != null)
                return;

            if (args.Target == null)
                return;

            if (_inventory.TryGetSlotEntity(args.Target.Value, "head", out var _))
                return;

            _audioSystem.PlayPvs(component.StartCageSound, args.Target.Value);

            component.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, 5, component.CancelToken.Token, args.Target.Value)
            {
                BroadcastFinishedEvent = new PutOnCageSuccessfulEvent(args.Target.Value, uid, args.User),
                BroadcastCancelledEvent = new PutOnCageCancelledEvent(args.Target.Value, uid, args.User),
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
                BreakOnStun = true,
                BreakOnDamage = true,
                NeedHand = true
            });
        }

        private void AddUncageVerb(EntityUid uid, HeadCagedComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    ResistCage(uid, args.User);
                },
                Text = Loc.GetString("cage-uncage-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
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

            _audioSystem.PlayPvs(cageComp.EndUncageSound, args.Caged);

            cageComp.CancelToken = null;

            RemComp<UnremoveableComponent>(args.Cage);
            if (_inventory.TryUnequip(args.Caged, "head", force: true))
                _alertsSystem.ClearAlert(args.Caged, AlertType.Caged);
        }

        private void OnPutOnCageCancelled(PutOnCageCancelledEvent args)
        {
            if (!EntityManager.TryGetComponent<HeadCageComponent>(args.Cage, out var cageComp))
                return;

            cageComp.CancelToken = null;
        }

        private void OnPutOnCageSuccessful(PutOnCageSuccessfulEvent args)
        {
            if (!EntityManager.TryGetComponent<HeadCageComponent>(args.Cage, out var cageComp))
                return;

            _audioSystem.PlayPvs(cageComp.EndCageSound, args.Caged);
            _inventory.TryEquip(args.Cager, args.Caged, args.Cage, "head");

            cageComp.CancelToken = null;
        }

        public void ResistCage(EntityUid caged, EntityUid uncager)
        {
            if (!_blocker.CanInteract(uncager, caged))
                return;

            if (!_inventory.TryGetSlotEntity(caged, "head", out var headItem) || !TryComp<HeadCageComponent>(headItem, out var cageComp))
            {
                _alertsSystem.ClearAlert(caged, AlertType.Caged);
                return;
            }

            if (cageComp.CancelToken != null)
                return;

            _audioSystem.PlayPvs(cageComp.StartBreakoutSound, caged);
            float doAfterLength = 5f;

            if (uncager == caged)
            {
                doAfterLength *= 2f;
                _popupSystem.PopupEntity(Loc.GetString("cage-resist-second-person", ("cage", headItem)), caged, caged, Shared.Popups.PopupType.Medium);
                _popupSystem.PopupEntity(Loc.GetString("cage-resist-third-person", ("user", Identity.Entity(caged, EntityManager)), ("cage", headItem)), caged, Filter.PvsExcept(caged), true, Shared.Popups.PopupType.MediumCaution);
            }

            cageComp.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(uncager, doAfterLength, cageComp.CancelToken.Token, caged)
            {
                BroadcastFinishedEvent = new ResistCageSuccessfulEvent(caged, headItem.Value),
                BroadcastCancelledEvent = new ResistCageCancelledEvent(caged, headItem.Value),
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
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

        private sealed class PutOnCageCancelledEvent : EntityEventArgs
        {
            public EntityUid Caged;
            public EntityUid Cage;
            public EntityUid Cager;
            public PutOnCageCancelledEvent(EntityUid caged, EntityUid cage, EntityUid cager)
            {
                Caged = caged;
                Cage = cage;
                Cager = cager;
            }
        }

        private sealed class PutOnCageSuccessfulEvent : EntityEventArgs
        {
            public EntityUid Caged;
            public EntityUid Cage;
            public EntityUid Cager;
            public PutOnCageSuccessfulEvent(EntityUid caged, EntityUid cage, EntityUid cager)
            {
                Caged = caged;
                Cage = cage;
                Cager = cager;
            }
        }

    }
}
