using System.Threading;
using Content.Shared.Arachne;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Content.Shared.Buckle.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Stunnable;
using Content.Shared.Eye.Blinding;
using Content.Shared.Doors.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Server.Buckle.Systems;
using Content.Server.Coordinates.Helpers;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.DoAfter;
using Content.Server.Body.Components;
using Content.Server.Vampiric;
using Content.Server.Speech.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Server.GameObjects;
using Robust.Server.Console;
using static Content.Shared.Examine.ExamineSystemShared;

namespace Content.Server.Arachne
{
    public sealed class ArachneSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly ThirstSystem _thirstSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly BuckleSystem _buckleSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly SharedBlindingSystem _blindingSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly IServerConsoleHost _host = default!;
        [Dependency] private readonly BloodSuckerSystem _bloodSuckerSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        private const string BodySlot = "body_slot";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ArachneComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ArachneComponent, GetVerbsEvent<InnateVerb>>(AddCocoonVerb);
            SubscribeLocalEvent<WebComponent, ComponentInit>(OnWebInit);
            SubscribeLocalEvent<WebComponent, GetVerbsEvent<AlternativeVerb>>(AddRestVerb);
            SubscribeLocalEvent<WebComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<CocoonComponent, EntInsertedIntoContainerMessage>(OnCocEntInserted);
            SubscribeLocalEvent<CocoonComponent, EntRemovedFromContainerMessage>(OnCocEntRemoved);
            SubscribeLocalEvent<CocoonComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<CocoonComponent, GetVerbsEvent<AlternativeVerb>>(AddSuccVerb);
            SubscribeLocalEvent<SpinWebActionEvent>(OnSpinWeb);
            SubscribeLocalEvent<WebSuccessfulEvent>(OnWebSuccessful);
            SubscribeLocalEvent<WebCancelledEvent>(OnWebCancelled);
            SubscribeLocalEvent<CocoonSuccessfulEvent>(OnCocoonSuccessful);
            SubscribeLocalEvent<CocoonCancelledEvent>(OnCocoonCancelled);
        }

        private void OnInit(EntityUid uid, ArachneComponent component, ComponentInit args)
        {
            if (_prototypeManager.TryIndex<WorldTargetActionPrototype>("SpinWeb", out var spinWeb))
                _actions.AddAction(uid, new WorldTargetAction(spinWeb), null);
        }

        private void AddCocoonVerb(EntityUid uid, ArachneComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (component.CancelToken != null)
                return;

            if (args.Target == uid)
                return;

            if (!TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
                return;

            if (bloodstream.BloodReagent != component.WebBloodReagent)
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StartCocooning(uid, component, args.Target);
                },
                Text = Loc.GetString("cocoon"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnWebInit(EntityUid uid, WebComponent component, ComponentInit args)
        {
            if (TryComp<StrapComponent>(uid, out var strap))
                _buckleSystem.StrapSetEnabled(uid, false, strap);
        }

        private void OnBuckleChange(EntityUid uid, WebComponent component, BuckleChangeEvent args)
        {
            if (!TryComp<StrapComponent>(uid, out var strap))
                return;

            if (!args.Buckling)
                _buckleSystem.StrapSetEnabled(uid, false, strap);
        }

        private void OnCocEntInserted(EntityUid uid, CocoonComponent component, EntInsertedIntoContainerMessage args)
        {
            _blindingSystem.AdjustBlindSources(args.Entity, 1);
            EnsureComp<StunnedComponent>(args.Entity);

            if (TryComp<ReplacementAccentComponent>(args.Entity, out var currentAccent))
            {
                component.WasReplacementAccent = true;
                component.OldAccent = currentAccent.Accent;
                currentAccent.Accent = "mumble";
            } else
            {
                component.WasReplacementAccent = false;
                var replacement = EnsureComp<ReplacementAccentComponent>(args.Entity);
                replacement.Accent = "mumble";
            }
        }

        private void OnCocEntRemoved(EntityUid uid, CocoonComponent component, EntRemovedFromContainerMessage args)
        {
            if (component.WasReplacementAccent && TryComp<ReplacementAccentComponent>(args.Entity, out var replacement))
            {
                replacement.Accent = component.OldAccent;
            } else
            {
                RemComp<ReplacementAccentComponent>(args.Entity);
            }

            RemComp<StunnedComponent>(args.Entity);
            _blindingSystem.AdjustBlindSources(args.Entity, -1);
        }

        private void OnDamageChanged(EntityUid uid, CocoonComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased)
                return;

            if (args.DamageDelta == null)
                return;

            var body = _itemSlots.GetItemOrNull(uid, BodySlot);

            if (body == null)
                return;

            var damage = args.DamageDelta * component.DamagePassthrough;
            _damageableSystem.TryChangeDamage(body, damage);
        }

        private void AddSuccVerb(EntityUid uid, CocoonComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<BloodSuckerComponent>(args.User, out var sucker))
                return;

            if (!sucker.WebRequired)
                return;

            var victim = _itemSlots.GetItemOrNull(uid, BodySlot);

            if (victim == null)
                return;

            if (!TryComp<BloodstreamComponent>(victim, out var stream))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    _bloodSuckerSystem.StartSuccDoAfter(args.User, victim.Value, sucker, stream, false); // start doafter
                },
                Text = Loc.GetString("action-name-suck-blood"),
                IconTexture = "/Textures/Nyanotrasen/Icons/verbiconfangs.png",
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void AddRestVerb(EntityUid uid, WebComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<StrapComponent>(uid, out var strap) || strap.Enabled)
                return;

            if (!TryComp<BuckleComponent>(args.User, out var buckle))
                return;

            if (!HasComp<ArachneComponent>(args.User))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    _buckleSystem.StrapSetEnabled(uid, true, strap);
                    if (_prototypeManager.TryIndex<InstantActionPrototype>("Sleep", out var sleep))
                        _actions.RemoveAction(uid, new InstantAction(sleep), null);
                    _buckleSystem.TryBuckle(args.User, args.User, uid, buckle);
                },
                Text = Loc.GetString("rest-on-web"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnEntRemoved(EntityUid uid, WebComponent web, EntRemovedFromContainerMessage args)
        {
            if (!TryComp<StrapComponent>(uid, out var strap))
                return;

            if (HasComp<ArachneComponent>(args.Entity))
                _buckleSystem.StrapSetEnabled(uid, false, strap);
        }

        private void OnSpinWeb(SpinWebActionEvent args)
        {
            if (!TryComp<ArachneComponent>(args.Performer, out var arachne) || arachne.CancelToken != null)
                return;

            if (_containerSystem.IsEntityInContainer(args.Performer))
                return;

            TryComp<HungerComponent>(args.Performer, out var hunger);
            TryComp<ThirstComponent>(args.Performer, out var thirst);

            if (hunger != null && thirst != null)
            {
                if (hunger.CurrentHungerThreshold <= Shared.Nutrition.Components.HungerThreshold.Peckish)
                {
                    _popupSystem.PopupEntity(Loc.GetString("spin-web-action-hungry"), args.Performer, args.Performer, Shared.Popups.PopupType.MediumCaution);
                    return;
                }
                if (thirst.CurrentThirstThreshold <= ThirstThreshold.Thirsty)
                {
                    _popupSystem.PopupEntity(Loc.GetString("spin-web-action-thirsty"), args.Performer, args.Performer, Shared.Popups.PopupType.MediumCaution);
                    return;
                }
            }

            var coords = args.Target;
            if (!_mapManager.TryGetGrid(coords.GetGridUid(EntityManager), out var grid))
            {
                _popupSystem.PopupEntity(Loc.GetString("action-name-spin-web-space"), args.Performer, args.Performer, Shared.Popups.PopupType.MediumCaution);
                return;
            }

            foreach (var entity in coords.GetEntitiesInTile())
            {
                PhysicsComponent? physics = null; // We use this to check if it's impassable
                if ((HasComp<WebComponent>(entity)) || // Is there already a web there?
                    ((Resolve(entity, ref physics, false) && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0) // Is it impassable?
                    &&  !(TryComp<DoorComponent>(entity, out var door) && door.State != DoorState.Closed))) // Is it a door that's open and so not actually impassable?
                {
                    _popupSystem.PopupEntity(Loc.GetString("action-name-spin-web-blocked"), args.Performer, args.Performer, Shared.Popups.PopupType.MediumCaution);
                    return;
                }
            }

            _popupSystem.PopupEntity(Loc.GetString("spin-web-start-third-person", ("spider", Identity.Entity(args.Performer, EntityManager))), args.Performer,
            Filter.PvsExcept(args.Performer).RemoveWhereAttachedEntity(entity => !ExamineSystemShared.InRangeUnOccluded(args.Performer, entity, ExamineRange, null)),
            true,
            Shared.Popups.PopupType.MediumCaution);
            _popupSystem.PopupEntity(Loc.GetString("spin-web-start-second-person"), args.Performer, args.Performer, Shared.Popups.PopupType.Medium);
            arachne.CancelToken = new CancellationTokenSource();
            _doAfter.DoAfter(new DoAfterEventArgs(args.Performer, arachne.WebDelay, arachne.CancelToken.Token)
            {
                BroadcastFinishedEvent = new WebSuccessfulEvent(args.Performer, coords),
                BroadcastCancelledEvent = new WebCancelledEvent(args.Performer),
                BreakOnUserMove = true,
                BreakOnStun = true,
            });
        }

        private void StartCocooning(EntityUid uid, ArachneComponent component, EntityUid target)
        {
            if (component.CancelToken != null)
                return;

            _popupSystem.PopupEntity(Loc.GetString("cocoon-start-third-person", ("target", Identity.Entity(target, EntityManager)), ("spider", Identity.Entity(uid, EntityManager))), uid,
                // TODO: We need popup occlusion lmao
                Filter.PvsExcept(uid).RemoveWhereAttachedEntity(entity => !ExamineSystemShared.InRangeUnOccluded(uid, entity, ExamineRange, null)),
                true,
                Shared.Popups.PopupType.MediumCaution);

            _popupSystem.PopupEntity(Loc.GetString("cocoon-start-second-person", ("target", Identity.Entity(target, EntityManager))), uid, uid, Shared.Popups.PopupType.Medium);

            var delay = component.CocoonDelay;

            if (HasComp<KnockedDownComponent>(target))
                delay *= component.CocoonKnockdownMultiplier;

            component.CancelToken = new CancellationTokenSource();
            _doAfter.DoAfter(new DoAfterEventArgs(uid, delay, component.CancelToken.Token, target)
            {
                BroadcastFinishedEvent = new CocoonSuccessfulEvent(uid, target),
                BroadcastCancelledEvent = new CocoonCancelledEvent(uid),
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
                BreakOnStun = true,
            });
        }

        private void OnCocoonSuccessful(CocoonSuccessfulEvent args)
        {
            if (!EntityManager.TryGetComponent(args.Webber, out ArachneComponent? arachne))
                return;

            arachne.CancelToken = null;

            var spawnProto = HasComp<HumanoidComponent>(args.Target) ? "CocoonedHumanoid" : "CocoonSmall";
            Transform(args.Target).AttachToGridOrMap();
            var cocoon = Spawn(spawnProto, Transform(args.Target).Coordinates);

            if (!TryComp<ItemSlotsComponent>(cocoon, out var slots))
                return;

            // todo: our species should use scale visuals probably...
            if (spawnProto == "CocoonedHumanoid" && TryComp<SpriteComponent>(args.Target, out var sprite))
            {
                // why the fuck is this only available as a console command.
                _host.ExecuteCommand(null, "scale " + cocoon + " " + sprite.Scale.Y);
            } else if (TryComp<PhysicsComponent>(args.Target, out var physics))
            {
                var scale = Math.Clamp(1 / (35 / physics.FixturesMass), 0.35, 2.5);
                _host.ExecuteCommand(null, "scale " + cocoon + " " + scale);
            }

            _inventorySystem.TryUnequip(args.Target, "ears", true, true);

            _itemSlots.SetLock(cocoon, BodySlot, false, slots);
            _itemSlots.TryInsert(cocoon, BodySlot, args.Target, args.Webber);
            _itemSlots.SetLock(cocoon, BodySlot, true, slots);

            var impact = (spawnProto == "CocoonedHumanoid") ? LogImpact.High : LogImpact.Medium;

            _adminLogger.Add(LogType.Action, impact, $"{ToPrettyString(args.Webber):player} cocooned {ToPrettyString(args.Target):target}");

        }

        private void OnWebCancelled(WebCancelledEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Webber, out ArachneComponent? arachne))
                return;

            arachne.CancelToken = null;
        }

        private void OnCocoonCancelled(CocoonCancelledEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Webber, out ArachneComponent? arachne))
                return;

            arachne.CancelToken = null;
        }

        private void OnWebSuccessful(WebSuccessfulEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.Webber, out ArachneComponent? arachne))
                return;

            arachne.CancelToken = null;

            if (TryComp<HungerComponent>(ev.Webber, out var hunger))
                hunger.UpdateFood(-8);
            if (TryComp<ThirstComponent>(ev.Webber, out var thirst))
                _thirstSystem.UpdateThirst(thirst, -20);

            Spawn("ArachneWeb", ev.Coords.SnapToGrid());
            _popupSystem.PopupEntity(Loc.GetString("spun-web-third-person", ("spider", Identity.Entity(ev.Webber, EntityManager))), ev.Webber,
            Filter.PvsExcept(ev.Webber).RemoveWhereAttachedEntity(entity => !ExamineSystemShared.InRangeUnOccluded(ev.Webber, entity, ExamineRange, null)),
            true,
            Shared.Popups.PopupType.MediumCaution);
            _popupSystem.PopupEntity(Loc.GetString("spun-web-second-person"), ev.Webber, ev.Webber, Shared.Popups.PopupType.Medium);
        }

        private sealed class WebCancelledEvent : EntityEventArgs
        {
            public EntityUid Webber;

            public WebCancelledEvent(EntityUid webber)
            {
                Webber = webber;
            }
        }

        private sealed class WebSuccessfulEvent : EntityEventArgs
        {
            public EntityUid Webber;

            public EntityCoordinates Coords;

            public WebSuccessfulEvent(EntityUid webber, EntityCoordinates coords)
            {
                Webber = webber;
                Coords = coords;
            }
        }

        private sealed class CocoonCancelledEvent : EntityEventArgs
        {
            public EntityUid Webber;

            public CocoonCancelledEvent(EntityUid webber)
            {
                Webber = webber;
            }
        }

        private sealed class CocoonSuccessfulEvent : EntityEventArgs
        {
            public EntityUid Webber;

            public EntityUid Target;

            public CocoonSuccessfulEvent(EntityUid webber, EntityUid target)
            {
                Webber = webber;
                Target = target;
            }
        }
    }
    public sealed class SpinWebActionEvent : WorldTargetActionEvent {}
}
