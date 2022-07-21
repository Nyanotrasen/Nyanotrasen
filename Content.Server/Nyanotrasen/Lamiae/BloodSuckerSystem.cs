using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.Lamiae
{
    public sealed class BloodSuckerSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BloodSuckerComponent, DidEquipHandEvent>(OnEquippedHand);
        }

        private void OnEquippedHand(EntityUid uid, BloodSuckerComponent component, EquippedHandEvent args)
        {
            EntityUid? succEntity = null;
            if (HasComp<BloodstreamComponent>(args.Equipped))
                succEntity = args.Equipped;
            if (TryComp<HandVirtualItemComponent>(args.Equipped, out var virtualItem) && HasComp<BloodstreamComponent>(virtualItem.BlockingEntity))
                succEntity = virtualItem.BlockingEntity;

            if (succEntity == null)
                return;

            Succ(uid, succEntity.Value, component);
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
            {
                return;
            }

            if (!_solutionSystem.TryGetSolution(stomachList[0].Comp.Owner, StomachSystem.DefaultSolutionName, out var stomachSolution))
                return;

            _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked", ("target", victim)), bloodsucker, Filter.Pvs(bloodsucker), Shared.Popups.PopupType.Medium);

            var temp = _solutionSystem.SplitSolution(victim, bloodstream.BloodSolution, bloodsuckerComp.UnitsToSucc);
            _solutionSystem.TryAddSolution(bloodsucker, stomachSolution, temp);
        }
    }
}
