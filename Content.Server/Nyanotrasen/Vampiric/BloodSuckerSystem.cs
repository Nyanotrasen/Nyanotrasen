using Content.Shared.Verbs;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Administration.Logs;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Server.HealthExaminable;
using Content.Server.DoAfter;
using Content.Server.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Audio;

namespace Content.Server.Vampiric
{
    public sealed class BloodSuckerSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BloodSuckerComponent, GetVerbsEvent<InnateVerb>>(AddSuccVerb);
            SubscribeLocalEvent<BloodSuckedComponent, HealthBeingExaminedEvent>(OnHealthExamined);
            SubscribeLocalEvent<BloodSuckedComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<TargetSuckSuccessfulEvent>(OnSuckSuccessful);
            SubscribeLocalEvent<SuckCancelledEvent>(OnSuckCancelled);
        }

        private void AddSuccVerb(EntityUid uid, BloodSuckerComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (args.User == args.Target)
                return;
            if (component.WebRequired)
                return; // handled elsewhere
            if (!TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
                return;
            if (!args.CanAccess)
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StartSuccDoAfter(uid, args.Target, component, bloodstream); // start doafter
                },
                Text = Loc.GetString("action-name-suck-blood"),
                IconTexture = "/Textures/Nyanotrasen/Icons/verbiconfangs.png",
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnHealthExamined(EntityUid uid, BloodSuckedComponent component, HealthBeingExaminedEvent args)
        {
            args.Message.PushNewline();
            args.Message.AddMarkup(Loc.GetString("bloodsucked-health-examine", ("target", uid)));
        }

        private void OnDamageChanged(EntityUid uid, BloodSuckedComponent component, DamageChangedEvent args)
        {
            if (args.DamageIncreased)
                return;

            if (_prototypeManager.TryIndex<DamageGroupPrototype>("Brute", out var brute) && args.Damageable.Damage.TryGetDamageInGroup(brute, out var bruteTotal)
                && _prototypeManager.TryIndex<DamageGroupPrototype>("Airloss", out var airloss) && args.Damageable.Damage.TryGetDamageInGroup(airloss, out var airlossTotal))
            {
                if (bruteTotal == 0 && airlossTotal == 0)
                    RemComp<BloodSuckedComponent>(uid);
            }
        }

        public void StartSuccDoAfter(EntityUid bloodsucker, EntityUid victim, BloodSuckerComponent? bloodSuckerComponent = null, BloodstreamComponent? stream = null, bool doChecks = true)
        {
            if (!Resolve(bloodsucker, ref bloodSuckerComponent))
                return;

            if (!Resolve(victim, ref stream))
                return;

            if (doChecks)
            {
                if (!_interactionSystem.InRangeUnobstructed(bloodsucker, victim))
                {
                    return;
                }

                if (_inventorySystem.TryGetSlotEntity(victim, "head", out var headUid) && HasComp<PressureProtectionComponent>(headUid))
                {
                    _popups.PopupEntity(Loc.GetString("bloodsucker-fail-helmet", ("helmet", headUid)), victim, bloodsucker, Shared.Popups.PopupType.Medium);
                    return;
                }

                if (_inventorySystem.TryGetSlotEntity(bloodsucker, "mask", out var maskUid) &&
                    EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
                    blocker.Enabled)
                {
                    _popups.PopupEntity(Loc.GetString("bloodsucker-fail-mask", ("mask", maskUid)), victim, bloodsucker, Shared.Popups.PopupType.Medium);
                    return;
                }
            }

            if (bloodSuckerComponent.CancelToken != null)
                return;

            if (stream.BloodReagent != "Blood")
            {
                _popups.PopupEntity(Loc.GetString("bloodsucker-fail-not-blood", ("target", victim)), victim, bloodsucker, Shared.Popups.PopupType.Medium);
                return;
            }

            if (stream.BloodSolution.CurrentVolume <= 1)
            {
                if (HasComp<BloodSuckedComponent>(victim))
                    _popups.PopupEntity(Loc.GetString("bloodsucker-fail-no-blood-bloodsucked", ("target", victim)), victim, bloodsucker, Shared.Popups.PopupType.Medium);
                else
                    _popups.PopupEntity(Loc.GetString("bloodsucker-fail-no-blood", ("target", victim)), victim, bloodsucker, Shared.Popups.PopupType.Medium);

                return;
            }


            _popups.PopupEntity(Loc.GetString("bloodsucker-doafter-start-victim", ("sucker", bloodsucker)), victim, victim, Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("bloodsucker-doafter-start", ("target", victim)), victim, bloodsucker, Shared.Popups.PopupType.Medium);

            bloodSuckerComponent.CancelToken = new System.Threading.CancellationTokenSource();
            _doAfter.DoAfter(new DoAfterEventArgs(bloodsucker, bloodSuckerComponent.SuccDelay, bloodSuckerComponent.CancelToken.Token, target: victim)
            {
                BroadcastFinishedEvent = new TargetSuckSuccessfulEvent(bloodsucker, victim),
                BroadcastCancelledEvent = new SuckCancelledEvent(bloodsucker),
                BreakOnTargetMove = true,
                BreakOnUserMove = false,
                DistanceThreshold = 2f,
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
            var stomachList = _bodySystem.GetBodyOrganComponents<StomachComponent>(bloodsucker);
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
                _popups.PopupEntity(Loc.GetString("drink-component-try-use-drink-had-enough"), bloodsucker, bloodsucker, Shared.Popups.PopupType.MediumCaution);
                return;
            }

            _adminLogger.Add(Shared.Database.LogType.MeleeHit, Shared.Database.LogImpact.Medium, $"{ToPrettyString(bloodsucker):player} sucked blood from {ToPrettyString(victim):target}");

            // All good, succ time.
            SoundSystem.Play("/Audio/Items/drink.ogg", Filter.Pvs(bloodsucker), bloodsucker);
            _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked-victim", ("sucker", bloodsucker)), victim, victim, Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("bloodsucker-blood-sucked", ("target", victim)), bloodsucker, bloodsucker, Shared.Popups.PopupType.Medium);
            EnsureComp<BloodSuckedComponent>(victim);

            // Make everything actually ingest.
            var temp = _solutionSystem.SplitSolution(victim, bloodstream.BloodSolution, unitsToDrain);
            temp.DoEntityReaction(bloodsucker, Shared.Chemistry.Reagent.ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(stomachList[0].Comp.Owner, temp, stomachList[0].Comp);

            // Add a little pierce
            DamageSpecifier damage = new();
            damage.DamageDict.Add("Piercing", 1); // Slowly accumulate enough to gib after like half an hour

            _damageableSystem.TryChangeDamage(victim, damage, true, true);

            // Inject if we have it.
            if (!bloodsuckerComp.InjectWhenSucc)
                return;

            if (!_solutionSystem.TryGetInjectableSolution(victim, out var injectable))
                return;

            _solutionSystem.TryAddReagent(victim, injectable, bloodsuckerComp.InjectReagent, bloodsuckerComp.UnitsToInject, out var acceptedQuantity);
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
}
