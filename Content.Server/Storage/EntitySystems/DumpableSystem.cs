using System.Threading;
using Content.Shared.Interaction;
using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Content.Shared.Placeable;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Storage.EntitySystems
{
    public sealed class DumpableSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly DisposalUnitSystem _disposalUnitSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DumpableComponent, AfterInteractEvent>(OnAfterInteract, after: new[]{ typeof(StorageSystem) });
            SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<AlternativeVerb>>(AddDumpVerb);
            SubscribeLocalEvent<DumpableComponent, GetVerbsEvent<UtilityVerb>>(AddUtilityVerbs);
            SubscribeLocalEvent<DumpableComponent, DoAfterEvent>(OnDoAfter);
        }

        private void OnAfterInteract(EntityUid uid, DumpableComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (HasComp<DisposalUnitComponent>(args.Target) || HasComp<PlaceableSurfaceComponent>(args.Target))
                StartDoAfter(uid, args.Target.Value, args.User, component);
        }

        private void AddDumpVerb(EntityUid uid, DumpableComponent dumpable, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ServerStorageComponent>(uid, out var storage) || storage.StoredEntities == null || storage.StoredEntities.Count == 0)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartDoAfter(uid, null, args.User, dumpable);//Had multiplier of 0.6f
                },
                Text = Loc.GetString("dump-verb-name"),
                Icon = new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/VerbIcons/drop.svg.192dpi.png")),
            };
            args.Verbs.Add(verb);
        }

        private void AddUtilityVerbs(EntityUid uid, DumpableComponent dumpable, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ServerStorageComponent>(uid, out var storage) || storage.StoredEntities == null || storage.StoredEntities.Count == 0)
                return;

            if (HasComp<DisposalUnitComponent>(args.Target))
            {
                UtilityVerb verb = new()
                {
                    Act = () =>
                    {
                        StartDoAfter(uid, args.Target, args.User, dumpable);
                    },
                    Text = Loc.GetString("dump-disposal-verb-name", ("unit", args.Target)),
                    IconEntity = uid
                };
                args.Verbs.Add(verb);
            }

            if (HasComp<PlaceableSurfaceComponent>(args.Target))
            {
                UtilityVerb verb = new()
                {
                    Act = () =>
                    {
                        StartDoAfter(uid, args.Target, args.User, dumpable);
                    },
                    Text = Loc.GetString("dump-placeable-verb-name", ("surface", args.Target)),
                    IconEntity = uid
                };
                args.Verbs.Add(verb);
            }
        }

        public void StartDoAfter(EntityUid storageUid, EntityUid? targetUid, EntityUid userUid, DumpableComponent dumpable)
        {
            if (!TryComp<SharedStorageComponent>(storageUid, out var storage) || storage.StoredEntities == null)
                return;

            float delay = storage.StoredEntities.Count * (float) dumpable.DelayPerItem.TotalSeconds * dumpable.Multiplier;

            _doAfterSystem.DoAfter(new DoAfterEventArgs(userUid, delay, target: targetUid, used: storageUid)
            {
                RaiseOnTarget = false,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        private void OnDoAfter(EntityUid uid, DumpableComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || !TryComp<SharedStorageComponent>(uid, out var storage) || storage.StoredEntities == null)
                return;

            Queue<EntityUid> dumpQueue = new();
            foreach (var entity in storage.StoredEntities)
            {
                dumpQueue.Enqueue(entity);
            }

            foreach (var entity in dumpQueue)
            {
                var transform = Transform(entity);
                _container.AttachParentToContainerOrGrid(transform);
                transform.LocalPosition += _random.NextVector2Box() / 2;
                transform.LocalRotation = _random.NextAngle();
            }

            if (args.Args.Target == null)
                return;

            if (HasComp<DisposalUnitComponent>(args.Args.Target.Value))
            {
                foreach (var entity in dumpQueue)
                {
                    _disposalUnitSystem.DoInsertDisposalUnit(args.Args.Target.Value, entity, args.Args.User);
                }
                return;
            }

            if (HasComp<PlaceableSurfaceComponent>(args.Args.Target.Value))
            {
                foreach (var entity in dumpQueue)
                {
                    Transform(entity).LocalPosition = Transform(args.Args.Target.Value).LocalPosition + _random.NextVector2Box() / 4;
                }
            }
        }
    }
}
