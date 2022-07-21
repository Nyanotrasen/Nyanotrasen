using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Server.HealthExaminable;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;


namespace Content.Server.Lamiae
{
    public sealed class BloodSuckerSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BloodSuckerComponent, DidEquipHandEvent>(OnEquippedHand);
            SubscribeLocalEvent<BloodSuckerComponent, DidUnequipHandEvent>(OnUnequippedHand);
            SubscribeLocalEvent<BloodSuckerComponent, SuckBloodActionEvent>(OnSuckBlood);
            SubscribeLocalEvent<BloodSuckedComponent, HealthBeingExaminedEvent>(OnHealthExamined);
        }

        private void OnEquippedHand(EntityUid uid, BloodSuckerComponent component, DidEquipHandEvent args)
        {
            EntityUid? succEntity = null;
            if (HasComp<BloodstreamComponent>(args.Equipped))
                succEntity = args.Equipped;
            if (TryComp<HandVirtualItemComponent>(args.Equipped, out var virtualItem) && HasComp<BloodstreamComponent>(virtualItem.BlockingEntity))
                succEntity = virtualItem.BlockingEntity;

            if (succEntity == null)
                return;

            component.PotentialTarget = succEntity;
            if (_prototypeManager.TryIndex<InstantActionPrototype>("SuckBlood", out var suckBlood))
                _actionsSystem.AddAction(uid, new InstantAction(suckBlood), null);
        }

        private void OnUnequippedHand(EntityUid uid, BloodSuckerComponent component, DidUnequipHandEvent args)
        {
            if (args.Unequipped == component.PotentialTarget && _prototypeManager.TryIndex<InstantActionPrototype>("SuckBlood", out var suckBlood))
                _actionsSystem.RemoveAction(uid, suckBlood);
        }

        private void OnSuckBlood(EntityUid uid, BloodSuckerComponent component, SuckBloodActionEvent args)
        {
            if (component.PotentialTarget == null)
                return;

            Succ(uid, component.PotentialTarget.Value, component);
        }

        private void OnHealthExamined(EntityUid uid, BloodSuckedComponent component, HealthBeingExaminedEvent args)
        {
            args.Message.PushNewline();
            args.Message.AddMarkup(Loc.GetString("bloodsucked-health-examine", ("target", uid)));
        }

        public void Succ(EntityUid bloodsucker, EntityUid victim, BloodSuckerComponent? bloodsuckerComp = null, BloodstreamComponent? bloodstream = null)
        {
            // Is bloodsucker a bloodsucker?
            if (!Resolve(bloodsucker, ref bloodsuckerComp))
                return;

            // Does victim have a bloodstream?
            if (!Resolve(victim, ref bloodstream))
                return;

            // No blood left, yikes.
            if (bloodstream.BloodSolution.TotalVolume == 0)
                return;

            // Does bloodsucker have a stomach?
            var stomachList = _bodySystem.GetComponentsOnMechanisms<StomachComponent>(bloodsucker);
            if (stomachList.Count == 0)
                return;

            if (!_solutionSystem.TryGetSolution(stomachList[0].Comp.Owner, StomachSystem.DefaultSolutionName, out var stomachSolution))
                return;

            // Are we too full?
            var unitsToDrain = bloodsuckerComp.UnitsToSucc;

            if (stomachSolution.AvailableVolume < unitsToDrain)
                unitsToDrain = (float) stomachSolution.AvailableVolume;

            if (unitsToDrain <= 2)
            {
                _popups.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"), bloodsucker, Filter.Pvs(bloodsucker), Shared.Popups.PopupType.MediumCaution);
                return;
            }
            // All good, succ time.
            _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked", ("target", victim)), bloodsucker, Filter.Pvs(bloodsucker), Shared.Popups.PopupType.Medium);
            EnsureComp<BloodSuckedComponent>(victim);

            var temp = _solutionSystem.SplitSolution(victim, bloodstream.BloodSolution, unitsToDrain);
            temp.DoEntityReaction(bloodsucker, Shared.Chemistry.Reagent.ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(stomachList[0].Comp.Owner, temp, stomachList[0].Comp);
        }
    }

    public sealed class SuckBloodActionEvent : InstantActionEvent {}
}
