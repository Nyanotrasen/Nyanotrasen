using System.Threading;
using Content.Shared.Verbs;
using Content.Shared.Item;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.DoAfter;
using Robust.Shared.Containers;

namespace Content.Server.Item.PseudoItem
{
    public sealed class PseudoItemSystem : EntitySystem
    {
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly ItemSystem _itemSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PseudoItemComponent, GetVerbsEvent<InnateVerb>>(AddInsertVerb);
            SubscribeLocalEvent<PseudoItemComponent, GetVerbsEvent<AlternativeVerb>>(AddInsertAltVerb);
            SubscribeLocalEvent<PseudoItemComponent, EntGotRemovedFromContainerMessage>(OnEntRemoved);
            SubscribeLocalEvent<PseudoItemComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
            SubscribeLocalEvent<PseudoItemComponent, DropAttemptEvent>(OnDropAttempt);

            SubscribeLocalEvent<InsertSuccessfulEvent>(OnInsertSuccessful);
            SubscribeLocalEvent<InsertCancelledEvent>(OnInsertCancelled);
        }

        private void AddInsertVerb(EntityUid uid, PseudoItemComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!TryComp<ServerStorageComponent>(args.Target, out var targetStorage))
                return;

            if (component.Size > targetStorage.StorageCapacityMax - targetStorage.StorageUsed)
                return;

            if (Transform(args.Target).ParentUid == uid)
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    TryInsert(args.Target, uid, component, targetStorage);
                },
                Text = Loc.GetString("action-name-insert-self"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void AddInsertAltVerb(EntityUid uid, PseudoItemComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (args.User == args.Target)
                return;

            if (args.Hands == null)
                return;

            if (!TryComp<ServerStorageComponent>(args.Hands.ActiveHandEntity, out var targetStorage))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartInsertDoAfter(args.User, uid, targetStorage.Owner, component);
                },
                Text = Loc.GetString("action-name-insert-other", ("target", Identity.Entity(args.Target, EntityManager))),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnEntRemoved(EntityUid uid, PseudoItemComponent component, EntGotRemovedFromContainerMessage args)
        {
            RemComp<ItemComponent>(uid);
            component.Active = false;
        }

        private void OnGettingPickedUpAttempt(EntityUid uid, PseudoItemComponent component, GettingPickedUpAttemptEvent args)
        {
            if (args.User == args.Item)
                return;

            Transform(uid).AttachToGridOrMap();
            args.Cancel();
        }

        private void OnDropAttempt(EntityUid uid, PseudoItemComponent component, DropAttemptEvent args)
        {
            if (component.Active)
                args.Cancel();
        }

        private void OnInsertCancelled(InsertCancelledEvent ev)
        {
            if (!TryComp<PseudoItemComponent>(ev.ToInsert, out var pseudoItem))
                return;

            pseudoItem.CancelToken?.Cancel();
            pseudoItem.CancelToken = null;
        }

        private void OnInsertSuccessful(InsertSuccessfulEvent ev)
        {
            if (!TryComp<PseudoItemComponent>(ev.ToInsert, out var pseudoItem))
                return;
            pseudoItem.CancelToken?.Cancel();
            pseudoItem.CancelToken = null;

            TryInsert(ev.TargetStorage, ev.ToInsert, pseudoItem);
        }

        public void TryInsert(EntityUid storageUid, EntityUid toInsert, PseudoItemComponent component, ServerStorageComponent? storage = null)
        {
            if (!Resolve(storageUid, ref storage))
                return;

            if (component.Size > storage.StorageCapacityMax - storage.StorageUsed)
                return;

            var item = EnsureComp<ItemComponent>(toInsert);
            _itemSystem.SetSize(toInsert, component.Size, item);

            component.Active = true;

            if (!_storageSystem.Insert(storage.Owner, toInsert, storage))
            {
                component.Active = false;
                RemComp<ItemComponent>(toInsert);
            } else
            {
                Transform(storageUid).AttachToGridOrMap();
            }
        }
        private void StartInsertDoAfter(EntityUid inserter, EntityUid toInsert, EntityUid storageEntity, PseudoItemComponent? pseudoItem = null)
        {
            if (!Resolve(toInsert, ref pseudoItem))
                return;

            if (pseudoItem.CancelToken != null)
                return;

            pseudoItem.CancelToken = new CancellationTokenSource();
            _doAfter.DoAfter(new DoAfterEventArgs(inserter, 5f,  pseudoItem.CancelToken.Token, target: toInsert)
            {
                BroadcastFinishedEvent = new InsertSuccessfulEvent(toInsert, storageEntity, inserter),
                BroadcastCancelledEvent = new InsertCancelledEvent(toInsert),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        private sealed class InsertCancelledEvent : EntityEventArgs
        {
            public EntityUid ToInsert;

            public InsertCancelledEvent(EntityUid toInsert)
            {
                ToInsert = toInsert;
            }
        }

        private sealed class InsertSuccessfulEvent : EntityEventArgs
        {
            public EntityUid Inserter;
            public EntityUid ToInsert;
            public EntityUid TargetStorage;
            public InsertSuccessfulEvent(EntityUid toInsert, EntityUid targetStorage, EntityUid inserter)
            {
                ToInsert = toInsert;
                TargetStorage = targetStorage;
                Inserter = inserter;
            }
        }
    }
}
