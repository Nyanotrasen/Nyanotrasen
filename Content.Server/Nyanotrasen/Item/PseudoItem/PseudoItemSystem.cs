using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Item;
using Robust.Shared.Containers;

namespace Content.Server.Item.PseudoItem
{
    public sealed class PseudoItemSystem : EntitySystem
    {
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly ItemSystem _itemSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PseudoItemComponent, GetVerbsEvent<InnateVerb>>(AddInsertVerb);
            SubscribeLocalEvent<PseudoItemComponent, EntGotRemovedFromContainerMessage>(OnEntRemoved);
            SubscribeLocalEvent<PseudoItemComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
        }

        private void AddInsertVerb(EntityUid uid, PseudoItemComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (!args.CanAccess)
                return;

            if (!TryComp<ServerStorageComponent>(args.Target, out var targetStorage))
                return;

            if (component.Size > targetStorage.StorageCapacityMax - targetStorage.StorageUsed)
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    TryInsert(uid, component, targetStorage);
                },
                Text = Loc.GetString("action-name-insert-self"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnEntRemoved(EntityUid uid, PseudoItemComponent component, EntGotRemovedFromContainerMessage args)
        {
            RemComp<ItemComponent>(uid);
        }

        private void OnGettingPickedUpAttempt(EntityUid uid, PseudoItemComponent component, GettingPickedUpAttemptEvent args)
        {
            if (args.User == args.Item)
                return;

            Transform(uid).AttachToGridOrMap();
            args.Cancel();
        }

        public void TryInsert(EntityUid toInsert, PseudoItemComponent component, ServerStorageComponent storage)
        {
            if (component.Size > storage.StorageCapacityMax - storage.StorageUsed)
                return;

            var item = EnsureComp<ItemComponent>(toInsert);
            _itemSystem.SetSize(toInsert, component.Size, item);

            _storageSystem.Insert(storage.Owner, toInsert, storage);
        }

    }
}
