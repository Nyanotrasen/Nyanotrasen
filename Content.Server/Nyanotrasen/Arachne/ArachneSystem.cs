using Content.Shared.Arachne;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.IdentityManagement;
using Content.Server.Coordinates.Helpers;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Random;

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
            SubscribeLocalEvent<SpinWebActionEvent>(OnSpinWeb);
        }

        private void OnInit(EntityUid uid, ArachneComponent component, ComponentInit args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("SpinWeb", out var spinWeb))
                _actions.AddAction(uid, new InstantAction(spinWeb), null);
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
