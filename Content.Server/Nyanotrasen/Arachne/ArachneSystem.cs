using Content.Shared.Arachne;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Content.Shared.Buckle.Components;
using Content.Server.Coordinates.Helpers;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Buckle.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Containers;

namespace Content.Server.Arachne
{
    public sealed class ArachneSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly ThirstSystem _thirstSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ArachneComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WebComponent, ComponentInit>(OnWebInit);
            SubscribeLocalEvent<WebComponent, GetVerbsEvent<AlternativeVerb>>(AddRestVerb);
            SubscribeLocalEvent<WebComponent, BuckleChangeEvent>(OnBuckleChange);
            SubscribeLocalEvent<SpinWebActionEvent>(OnSpinWeb);
        }

        private void OnInit(EntityUid uid, ArachneComponent component, ComponentInit args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("SpinWeb", out var spinWeb))
                _actions.AddAction(uid, new InstantAction(spinWeb), null);
        }

        private void OnWebInit(EntityUid uid, WebComponent component, ComponentInit args)
        {
            if (TryComp<StrapComponent>(uid, out var strap))
                strap.Enabled = false;
        }

        private void OnBuckleChange(EntityUid uid, WebComponent component, BuckleChangeEvent args)
        {
            if (!TryComp<StrapComponent>(uid, out var strap))
                return;

            if (!args.Buckling)
                strap.Enabled = false;
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
                    strap.Enabled = true;
                    if (_prototypeManager.TryIndex<InstantActionPrototype>("Sleep", out var sleep))
                        _actions.RemoveAction(uid, new InstantAction(sleep), null);
                    buckle.TryBuckle(args.User, uid);
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
                strap.Enabled = false;
        }

        private void OnSpinWeb(SpinWebActionEvent args)
        {
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
                hunger.UpdateFood(-8);
                _thirstSystem.UpdateThirst(thirst, -20);
            }

            Spawn("ArachneWeb", Transform(args.Performer).Coordinates.SnapToGrid());
            _popupSystem.PopupEntity(Loc.GetString("spun-web-third-person", ("spider", Identity.Entity(args.Performer, EntityManager))), args.Performer, Filter.PvsExcept(args.Performer), Shared.Popups.PopupType.MediumCaution);
            _popupSystem.PopupEntity(Loc.GetString("spun-web-second-person"), args.Performer, Filter.Entities(args.Performer), Shared.Popups.PopupType.Medium);
            args.Handled = true;
        }
    }
    public sealed class SpinWebActionEvent : InstantActionEvent {}
}
