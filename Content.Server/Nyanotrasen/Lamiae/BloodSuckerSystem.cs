using Content.Shared.Verbs;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Inventory;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Server.HealthExaminable;
using Content.Server.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Audio;

namespace Content.Server.Lamiae
{
    public sealed class BloodSuckerSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BloodSuckerComponent, GetVerbsEvent<InnateVerb>>(AddSuccVerb);
            SubscribeLocalEvent<BloodSuckerComponent, DidEquipHandEvent>(OnEquippedHand);
            SubscribeLocalEvent<BloodSuckerComponent, DidUnequipHandEvent>(OnUnequippedHand);
            SubscribeLocalEvent<BloodSuckerComponent, SuckBloodActionEvent>(OnSuckBlood);
            SubscribeLocalEvent<BloodSuckedComponent, HealthBeingExaminedEvent>(OnHealthExamined);
            SubscribeLocalEvent<TargetSuckSuccessfulEvent>(OnSuckSuccessful);
            SubscribeLocalEvent<SuckCancelledEvent>(OnSuckCancelled);
        }

        private void AddSuccVerb(EntityUid uid, BloodSuckerComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (args.User == args.Target)
                return;
            if (!TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StartSuccDoAfter(uid, args.Target, component, bloodstream); // start doafter
                },
                Text = Loc.GetString("action-name-suck-blood"),
                IconTexture = "/Textures/Nyanotrasen/Mobs/Species/lamia.rsi/verbiconfangs.png",
                Priority = 2
            };
            args.Verbs.Add(verb);
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
            if (args.Unequipped == component.PotentialTarget
            || TryComp<HandVirtualItemComponent>(args.Unequipped, out var virtualItem) && virtualItem.BlockingEntity == component.PotentialTarget)
            {
                component.PotentialTarget = null;
                 if (_prototypeManager.TryIndex<InstantActionPrototype>("SuckBlood", out var suckBlood))
                    _actionsSystem.RemoveAction(uid, suckBlood);
            }
        }

        private void OnSuckBlood(EntityUid uid, BloodSuckerComponent component, SuckBloodActionEvent args)
        {
            if (component.PotentialTarget == null)
                return;

            StartSuccDoAfter(uid, component.PotentialTarget.Value, component);
        }

        private void OnHealthExamined(EntityUid uid, BloodSuckedComponent component, HealthBeingExaminedEvent args)
        {
            args.Message.PushNewline();
            args.Message.AddMarkup(Loc.GetString("bloodsucked-health-examine", ("target", uid)));
        }

        private void StartSuccDoAfter(EntityUid bloodsucker, EntityUid victim, BloodSuckerComponent? bloodSuckerComponent = null, BloodstreamComponent? stream = null)
        {
            if (!Resolve(bloodsucker, ref bloodSuckerComponent))
                return;

            if (!Resolve(victim, ref stream))
                return;

            if (_inventorySystem.TryGetSlotEntity(victim, "head", out var headUid) && HasComp<PressureProtectionComponent>(headUid))
            {
                _popups.PopupEntity(Loc.GetString("bloodsucker-fail-helmet", ("helmet", headUid)), victim, Filter.Entities(bloodsucker), Shared.Popups.PopupType.Medium);
                return;
            }

            if (bloodSuckerComponent.CancelToken != null)
                return;

            if (stream.BloodReagent != "Blood")
            {
                _popups.PopupEntity(Loc.GetString("bloodsucker-fail-not-blood", ("target", victim)), victim, Filter.Entities(bloodsucker), Shared.Popups.PopupType.Medium);
                return;
            }

            if (stream.BloodSolution.CurrentVolume <= 1)
            {
                if (HasComp<BloodSuckedComponent>(victim))
                    _popups.PopupEntity(Loc.GetString("bloodsucker-fail-no-blood-bloodsucked", ("target", victim)), victim, Filter.Entities(bloodsucker), Shared.Popups.PopupType.Medium);
                else
                    _popups.PopupEntity(Loc.GetString("bloodsucker-fail-no-blood", ("target", victim)), victim, Filter.Entities(bloodsucker), Shared.Popups.PopupType.Medium);

                return;
            }


            _popups.PopupEntity(Loc.GetString("bloodsucker-doafter-start-victim", ("sucker", bloodsucker)), victim, Filter.Entities(victim), Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("bloodsucker-doafter-start", ("target", victim)), victim, Filter.Entities(bloodsucker), Shared.Popups.PopupType.Medium);

            bloodSuckerComponent.CancelToken = new System.Threading.CancellationTokenSource();
            _doAfter.DoAfter(new DoAfterEventArgs(bloodsucker, bloodSuckerComponent.SuccDelay, bloodSuckerComponent.CancelToken.Token, target: victim)
            {
                BroadcastFinishedEvent = new TargetSuckSuccessfulEvent(bloodsucker, victim),
                BroadcastCancelledEvent = new SuckCancelledEvent(bloodsucker),
                BreakOnTargetMove = true,
                BreakOnUserMove = false,
                BreakOnStun = true,
                NeedHand = false
            });
        }

        private void OnSuckSuccessful(TargetSuckSuccessfulEvent ev)
        {
            if (!TryComp<BloodSuckerComponent>(ev.Sucker, out var succComp))
                return;

            succComp.CancelToken = null;

            Succ(ev.Sucker, ev.Target, succComp);
        }

        private void OnSuckCancelled(SuckCancelledEvent ev)
        {
            if (!TryComp<BloodSuckerComponent>(ev.Sucker, out var succComp))
                return;

            succComp.CancelToken = null;
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
                _popups.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"), bloodsucker, Filter.Entities(bloodsucker), Shared.Popups.PopupType.MediumCaution);
                return;
            }
            // All good, succ time.
            SoundSystem.Play("/Audio/Items/drink.ogg", Filter.Pvs(bloodsucker));
            _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked-victim", ("sucker", bloodsucker)), victim, Filter.Entities(victim), Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked", ("target", victim)), bloodsucker, Filter.Entities(bloodsucker), Shared.Popups.PopupType.Medium);
            EnsureComp<BloodSuckedComponent>(victim);

            // Make everything actually ingest.
            var temp = _solutionSystem.SplitSolution(victim, bloodstream.BloodSolution, unitsToDrain);
            temp.DoEntityReaction(bloodsucker, Shared.Chemistry.Reagent.ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(stomachList[0].Comp.Owner, temp, stomachList[0].Comp);
        }

        private sealed class SuckCancelledEvent : EntityEventArgs
        {
            public EntityUid Sucker;

            public SuckCancelledEvent(EntityUid sucker)
            {
                Sucker = sucker;
            }
        }

        private sealed class TargetSuckSuccessfulEvent : EntityEventArgs
        {
            public EntityUid Sucker;
            public EntityUid Target;
            public TargetSuckSuccessfulEvent(EntityUid sucker, EntityUid target)
            {
                Sucker = sucker;
                Target = target;
            }
        }
    }

    public sealed class SuckBloodActionEvent : InstantActionEvent {}
}
