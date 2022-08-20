using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Storage;
using Content.Shared.Item;

namespace Content.Server.Item.PseudoItem
{
    public sealed class PseudoItemSystem : EntitySystem
    {
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PseudoItemComponent, GetVerbsEvent<InnateVerb>>(AddInsertVerb);
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

        public void TryInsert(EntityUid toInsert, PseudoItemComponent component, ServerStorageComponent storage)
        {
            if (component.Size > storage.StorageCapacityMax - storage.StorageUsed)
                return;

            var item = EnsureComp<ItemComponent>(toInsert);

            _storageSystem.Insert(storage.Owner, toInsert, storage);
        }

    }
}
