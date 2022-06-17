using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Content.Server.Body.Components;
using Content.Server.Medical;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Inventory;
using Content.Server.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Abilities.Felinid
{
    public sealed class FelinidSystem : EntitySystem
    {

        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly VomitSystem _vomitSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FelinidComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<FelinidComponent, HairballActionEvent>(OnHairball);
            SubscribeLocalEvent<HairballComponent, ThrowDoHitEvent>(OnHairballHit);
            SubscribeLocalEvent<HairballComponent, GettingPickedUpAttemptEvent>(OnHairballPickupAttempt);
        }

        private Queue<EntityUid> RemQueue = new();

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var cat in RemQueue)
            {
                RemComp<CoughingUpHairballComponent>(cat);
            }
            RemQueue.Clear();

            foreach (var (hairballComp, catComp) in EntityQuery<CoughingUpHairballComponent, FelinidComponent>())
            {
                hairballComp.Accumulator += frameTime;
                if (hairballComp.Accumulator < hairballComp.CoughUpTime.TotalSeconds)
                    continue;

                hairballComp.Accumulator = 0;
                SpawnHairball(hairballComp.Owner, catComp);
                RemQueue.Enqueue(hairballComp.Owner);
            }
        }

        private void OnInit(EntityUid uid, FelinidComponent component, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, component.HairballAction, uid);
        }

        private void OnHairball(EntityUid uid, FelinidComponent component, HairballActionEvent args)
        {
            if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
            EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
            blocker.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("hairball-mask", ("mask", maskUid)), uid, Filter.Entities(uid));
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("hairball-cough", ("name", uid)), uid, Filter.Pvs(uid));
            SoundSystem.Play("/Audio/Effects/Species/hairball.ogg", Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.15f));

            AddComp<CoughingUpHairballComponent>(uid);
            args.Handled = true;
        }

        private void SpawnHairball(EntityUid uid, FelinidComponent component)
        {
            var hairball = EntityManager.SpawnEntity(component.HairballPrototype, Transform(uid).Coordinates);
            var hairballComp = Comp<HairballComponent>(hairball);

            if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
            {
                var temp = bloodstream.ChemicalSolution.SplitSolution(20);

                if (_solutionSystem.TryGetSolution(hairball, hairballComp.SolutionName, out var hairballSolution))
                {
                    _solutionSystem.TryAddSolution(hairball, hairballSolution, temp);
                }
            }
        }
        private void OnHairballHit(EntityUid uid, HairballComponent component, ThrowDoHitEvent args)
        {
            if (HasComp<FelinidComponent>(args.Target) || !HasComp<StatusEffectsComponent>(args.Target))
                return;
            if (_robustRandom.Prob(0.2f))
                _vomitSystem.Vomit(args.Target);
        }

        private void OnHairballPickupAttempt(EntityUid uid, HairballComponent component, GettingPickedUpAttemptEvent args)
        {
            if (HasComp<FelinidComponent>(args.User) || !HasComp<StatusEffectsComponent>(args.User))
                return;

            if (_robustRandom.Prob(0.2f))
            {
                _vomitSystem.Vomit(args.User);
                args.Cancel();
            }
        }
    }

    public sealed class HairballActionEvent : InstantActionEvent {}
}
