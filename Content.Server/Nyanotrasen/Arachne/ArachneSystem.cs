using System.Threading;
using Content.Shared.Arachne;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Content.Shared.Buckle.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Doors.Components;
using Content.Server.Buckle.Systems;
using Content.Server.Coordinates.Helpers;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Buckle.Components;
using Content.Server.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;

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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ArachneComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WebComponent, ComponentInit>(OnWebInit);
            SubscribeLocalEvent<WebComponent, GetVerbsEvent<AlternativeVerb>>(AddRestVerb);
            SubscribeLocalEvent<WebComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<SpinWebActionEvent>(OnSpinWeb);
            SubscribeLocalEvent<WebSuccessfulEvent>(OnWebSuccessful);
            SubscribeLocalEvent<WebCancelledEvent>(OnWebCancelled);
        }

        private void OnInit(EntityUid uid, ArachneComponent component, ComponentInit args)
        {
            if (_prototypeManager.TryIndex<WorldTargetActionPrototype>("SpinWeb", out var spinWeb))
                _actions.AddAction(uid, new WorldTargetAction(spinWeb), null);
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

            TryComp<HungerComponent>(args.Performer, out var hunger);
            TryComp<ThirstComponent>(args.Performer, out var thirst);

            if (hunger != null && thirst != null)
            {
                if (hunger.CurrentHungerThreshold <= Shared.Nutrition.Components.HungerThreshold.Peckish)
                {
                    _popupSystem.PopupEntity(Loc.GetString("spin-web-action-hungry"), args.Performer, Filter.Entities(args.Performer), Shared.Popups.PopupType.MediumCaution);
                    return;
                }
                if (thirst.CurrentThirstThreshold <= ThirstThreshold.Thirsty)
                {
                    _popupSystem.PopupEntity(Loc.GetString("spin-web-action-thirsty"), args.Performer, Filter.Entities(args.Performer), Shared.Popups.PopupType.MediumCaution);
                    return;
                }
            }

            var coords = args.Target;
            if (!_mapManager.TryGetGrid(coords.GetGridUid(EntityManager), out var grid))
            {
                _popupSystem.PopupEntity(Loc.GetString("action-name-spin-web-space"), args.Performer, Filter.Entities(args.Performer), Shared.Popups.PopupType.MediumCaution);
                return;
            }

            foreach (var entity in coords.GetEntitiesInTile())
            {
                IPhysBody? physics = null; // We use this to check if it's impassable
                if ((HasComp<WebComponent>(entity)) || // Is there already a web there?
                    ((Resolve(entity, ref physics, false) && (physics.CollisionLayer & (int) CollisionGroup.Impassable) != 0) // Is it impassable?
                    &&  !(TryComp<DoorComponent>(entity, out var door) && door.State != DoorState.Closed))) // Is it a door that's open and so not actually impassable?
                {
                    _popupSystem.PopupEntity(Loc.GetString("action-name-spin-web-blocked"), args.Performer, Filter.Entities(args.Performer), Shared.Popups.PopupType.MediumCaution);
                    return;
                }
            }

            _popupSystem.PopupEntity(Loc.GetString("spin-web-start-third-person", ("spider", Identity.Entity(args.Performer, EntityManager))), args.Performer, Filter.PvsExcept(args.Performer), Shared.Popups.PopupType.MediumCaution);
            _popupSystem.PopupEntity(Loc.GetString("spin-web-start-second-person"), args.Performer, Filter.Entities(args.Performer), Shared.Popups.PopupType.Medium);
            arachne.CancelToken = new CancellationTokenSource();
            _doAfter.DoAfter(new DoAfterEventArgs(args.Performer, arachne.WebDelay, arachne.CancelToken.Token)
            {
                BroadcastFinishedEvent = new WebSuccessfulEvent(args.Performer, coords),
                BroadcastCancelledEvent = new WebCancelledEvent(args.Performer),
                BreakOnUserMove = true,
                BreakOnStun = true,
            });
        }

        private void OnWebCancelled(WebCancelledEvent ev)
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
            _popupSystem.PopupEntity(Loc.GetString("spun-web-third-person", ("spider", Identity.Entity(ev.Webber, EntityManager))), ev.Webber, Filter.PvsExcept(ev.Webber), Shared.Popups.PopupType.MediumCaution);
            _popupSystem.PopupEntity(Loc.GetString("spun-web-second-person"), ev.Webber, Filter.Entities(ev.Webber), Shared.Popups.PopupType.Medium);
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
    }
    public sealed class SpinWebActionEvent : WorldTargetActionEvent {}
}
