using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Content.Server.Mail.Components;
using Content.Server.Power.Components;
using Content.Server.Popups;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Damage.Components;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Maps;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mail;
using Content.Shared.PDA;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Containers;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Mail
{
    public sealed class MailSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly IdCardSystem _idCardSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly CargoSystem _cargoSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MailComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<MailComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<MailComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<MailComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<MailComponent, DestructionEventArgs>(OnDestruction);
            SubscribeLocalEvent<MailComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<MailComponent, BreakageEventArgs>(OnBreak);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var mailTeleporter in EntityQuery<MailTeleporterComponent>())
            {
                if (TryComp<ApcPowerReceiverComponent>(mailTeleporter.Owner, out var power) && !power.Powered)
                    return;

                mailTeleporter.Accumulator += frameTime;

                if (mailTeleporter.Accumulator < mailTeleporter.teleportInterval.TotalSeconds)
                    continue;

                mailTeleporter.Accumulator -= (float) mailTeleporter.teleportInterval.TotalSeconds;

                SpawnMail(mailTeleporter.Owner, mailTeleporter);
            }
        }

        private void OnRemove(EntityUid uid, MailComponent component, ComponentRemove args)
        {
            // Make sure the priority timer doesn't run.
            if (component.priorityCancelToken != null)
                component.priorityCancelToken.Cancel();
        }

        /// <summary>
        /// Try to open the mail.
        /// <summary>
        private void OnUseInHand(EntityUid uid, MailComponent component, UseInHandEvent args)
        {
            if (!component.Enabled)
                return;
            if (component.Locked)
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-locked"), uid, Filter.Entities(args.User));
                return;
            }
            OpenMail(uid, component, args.User);
        }

        /// <summary>
        /// Check the ID against the mail's lock
        /// </summary>
        private void OnAfterInteractUsing(EntityUid uid, MailComponent component, AfterInteractUsingEvent args)
        {
            if (!args.CanReach || !component.Locked)
                return;

            if (!TryComp<AccessReaderComponent>(uid, out var access))
                return;

            IdCardComponent? idCard = null; // We need an ID card.

            if (HasComp<PDAComponent>(args.Used)) /// Can we find it in a PDA if the user is using that?
            {
                _idCardSystem.TryGetIdCard(args.Used, out var pdaID);
                idCard = pdaID;
            }

            if (HasComp<IdCardComponent>(args.Used)) /// Or are they using an id card directly?
                idCard = Comp<IdCardComponent>(args.Used);

            if (idCard == null) /// Return if we still haven't found an id card.
                return;


            if (idCard.FullName != component.Recipient || idCard.JobTitle != component.RecipientJob)
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-recipient-mismatch"), uid, Filter.Entities(args.User));
                return;
            }

            if (!_accessSystem.IsAllowed(uid, args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-invalid-access"), uid, Filter.Entities(args.User));
                return;
            }

            component.Locked = false;
            UpdateAntiTamperVisuals(uid, false);

            if (component.IsPriority)
            {
                // This is a successful delivery. Keep the failure timer from triggering.
                if (component.priorityCancelToken != null)
                    component.priorityCancelToken.Cancel();

                // The priority tape is visually considered to be a part of the
                // anti-tamper lock, so remove that too.
                _appearanceSystem.SetData(uid, MailVisuals.IsPriority, false);

                // The examination code depends on this being false to not show
                // the priority tape description anymore.
                component.IsPriority = false;
            }

            if (!component.Profitable)
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-unlocked"), uid, Filter.Entities(args.User));
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("mail-unlocked-reward", ("bounty", component.Bounty)), uid, Filter.Entities(args.User));

            component.Profitable = false;

            foreach (var account in EntityQuery<StationBankAccountComponent>())
            {
                if (_stationSystem.GetOwningStation(account.Owner) != _stationSystem.GetOwningStation(uid))
                        continue;

                _cargoSystem.UpdateBankAccount(account, component.Bounty);
                return;
            }
        }

        private void OnExamined(EntityUid uid, MailComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("mail-desc-far"));
                return;
            }

            args.PushMarkup(Loc.GetString("mail-desc-close", ("name", component.Recipient), ("job", component.RecipientJob)));

            if (component.IsFragile)
                args.PushMarkup(Loc.GetString("mail-desc-fragile"));

            if (component.IsPriority)
            {
                if (component.Profitable)
                    args.PushMarkup(Loc.GetString("mail-desc-priority"));
                else
                    args.PushMarkup(Loc.GetString("mail-desc-priority-inactive"));
            }
        }

        /// <summary>
        /// Penalize a station for a failed delivery.
        /// </summary>
        /// <remarks>
        /// This will mark a parcel as no longer being profitable, which will
        /// prevent multiple failures on different conditions for the same
        /// delivery.
        ///
        /// The standard penalization is breaking the anti-tamper lock,
        /// but this allows a delivery to fail for other reasons too
        /// while having a generic function to handle different messages.
        /// </remarks>
        private void PenalizeStationFailedDelivery(EntityUid uid, MailComponent component, string localizationString)
        {
            if (!component.Profitable)
                return;

            _chatSystem.TrySendInGameICMessage(uid, Loc.GetString(localizationString, ("credits", component.Penalty)), InGameICChatType.Speak, false);
            _audioSystem.PlayPvs(component.PenaltySound, uid);

            component.Profitable = false;

            if (component.IsPriority)
                _appearanceSystem.SetData(uid, MailVisuals.IsPriorityInactive, true);

            foreach (var account in EntityQuery<StationBankAccountComponent>())
            {
                if (_stationSystem.GetOwningStation(account.Owner) != _stationSystem.GetOwningStation(uid))
                        continue;

                _cargoSystem.UpdateBankAccount(account, component.Penalty);
                return;
            }
        }

        private void OnDestruction(EntityUid uid, MailComponent component, DestructionEventArgs args)
        {
            if (component.Locked)
                PenalizeStationFailedDelivery(uid, component, "mail-penalty-lock");

            if (component.Enabled)
                OpenMail(uid, component);

            UpdateAntiTamperVisuals(uid, false);
        }

        private void OnDamage(EntityUid uid, MailComponent component, DamageChangedEvent args)
        {
            if (args.DamageDelta == null)
                return;

            if (!_containerSystem.TryGetContainer(uid, "contents", out var contents))
                return;

            // Transfer damage to the contents.
            // This should be a general-purpose feature for all containers in the future.
            foreach (var entity in contents.ContainedEntities.ToArray())
            {
                var result = _damageableSystem.TryChangeDamage(entity, args.DamageDelta);
                if (result != null)
                    Logger.Debug($"Mail transferred damage result: {result.Total}");
            }
        }

        private void OnBreak(EntityUid uid, MailComponent component, BreakageEventArgs args)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsBroken, true);

            if (component.IsFragile)
                PenalizeStationFailedDelivery(uid, component, "mail-penalty-fragile");
        }

        /// <summary>
        /// Returns true if the given entity is considered fragile for delivery.
        /// </summary>
        public bool IsEntityFragile(EntityUid uid, int fragileDamageThreshold)
        {
            // It takes damage on falling.
            if (HasComp<DamageOnLandComponent>(uid))
                return true;

            // It can be spilled easily and has something to spill.
            if (HasComp<SpillableComponent>(uid)
                && TryComp(uid, out DrinkComponent? drinkComponent)
                && drinkComponent.Opened
                && _solutionContainerSystem.PercentFull(uid) > 0)
                return true;

            // It might be made of non-reinforced glass.
            if (TryComp(uid, out DamageableComponent? damageableComponent)
                && damageableComponent.DamageModifierSetId == "Glass")
                return true;

            // Fallback: It breaks or is destroyed in less than a damage
            // threshold dictated by the teleporter.
            if (TryComp(uid, out DestructibleComponent? destructibleComp))
            {
                foreach (var threshold in destructibleComp.Thresholds)
                {
                    if (threshold.Trigger is DamageTrigger trigger
                        && trigger.Damage < fragileDamageThreshold)
                    {
                        foreach (var behavior in threshold.Behaviors)
                        {
                            if (behavior is DoActsBehavior doActs)
                            {
                                if (doActs.Acts.HasFlag(ThresholdActs.Breakage)
                                    || doActs.Acts.HasFlag(ThresholdActs.Destruction))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public bool TryMatchJobTitleToDepartment(string jobTitle, [NotNullWhen(true)] out string? jobDepartment)
        {
            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                foreach (var role in department.Roles)
                {
                    if (_prototypeManager.TryIndex(role, out JobPrototype? _jobPrototype)
                        && _jobPrototype.LocalizedName == jobTitle)
                    {
                        jobDepartment = department.ID;
                        return true;
                    }
                }
            }

            Logger.Debug($"Was unable to find Department for jobTitle: {jobTitle}");

            jobDepartment = null;
            return false;
        }

        public bool TryMatchJobTitleToIcon(string jobTitle, [NotNullWhen(true)] out string? jobIcon)
        {
            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>())
            {
                if (job.LocalizedName == jobTitle)
                {
                    jobIcon = job.Icon;
                    return true;
                }
            }

            Logger.Debug($"Was unable to find Icon for jobTitle: {jobTitle}");

            jobIcon = null;
            return false;
        }

        /// <summary>
        /// Handle all the gritty details particular to a new mail entity.
        /// </summary>
        /// <remarks>
        /// This is separate mostly so the unit tests can get to it.
        /// </remarks>
        public void SetupMail(EntityUid uid, MailTeleporterComponent component, string recipientName, string recipientJob, HashSet<String> accessTags)
        {
            var mailComp = EnsureComp<MailComponent>(uid);

            var container = _containerSystem.EnsureContainer<Container>(uid, "contents", out var contents);
            foreach (var item in EntitySpawnCollection.GetSpawns(mailComp.Contents, _random))
            {
                var entity = EntityManager.SpawnEntity(item, Transform(uid).Coordinates);
                if (!container.Insert(entity))
                {
                    Logger.Error($"Can't insert {ToPrettyString(entity)} into new mail delivery {ToPrettyString(uid)}! Deleting it.");
                    QueueDel(entity);
                }
                else if (!mailComp.IsFragile && IsEntityFragile(entity, component.FragileDamageThreshold))
                {
                    mailComp.IsFragile = true;

                    Logger.Debug($"Spawned a fragile entity {ToPrettyString(entity)} for mail {uid}");
                }
            }

            if (_random.Prob(component.PriorityChance))
                mailComp.IsPriority = true;

            mailComp.RecipientJob = recipientJob;
            mailComp.Recipient = recipientName;

            if (mailComp.IsFragile)
            {
                mailComp.Bounty += component.FragileBonus;
                mailComp.Penalty += component.FragileMalus;
                _appearanceSystem.SetData(uid, MailVisuals.IsFragile, true);
            }

            if (mailComp.IsPriority)
            {
                mailComp.Bounty += component.PriorityBonus;
                mailComp.Penalty += component.PriorityMalus;
                _appearanceSystem.SetData(uid, MailVisuals.IsPriority, true);

                mailComp.priorityCancelToken = new CancellationTokenSource();

                Timer.Spawn((int) component.priorityDuration.TotalMilliseconds,
                    () => PenalizeStationFailedDelivery(uid, mailComp, "mail-penalty-expired"),
                    mailComp.priorityCancelToken.Token);

                Logger.Debug($"{ToPrettyString(uid)} has been marked as priority mail");
            }

            if (TryMatchJobTitleToIcon(recipientJob, out string? icon))
                _appearanceSystem.SetData(uid, MailVisuals.JobIcon, icon);

            var accessReader = EnsureComp<AccessReaderComponent>(uid);
            accessReader.AccessLists.Add(accessTags);
        }

        /// <summary>
        /// Return how many parcels are waiting for delivery.
        /// </summary>
        public uint GetUndeliveredParcelCount(EntityUid uid)
        {
            // An alternative solution would be to keep a list of the unopened
            // parcels spawned by the teleporter and see if they're not carried
            // by someone, but this is simple, and simple is good.
            uint undeliveredParcelCount = 0;
            foreach (var entityInTile in TurfHelpers.GetEntitiesInTile(Transform(uid).Coordinates))
            {
                if (HasComp<MailComponent>(entityInTile))
                    undeliveredParcelCount++;
            }
            return undeliveredParcelCount;
        }

        /// <summary>
        /// Handle the spawning of all the mail for a mail teleporter.
        /// </summary>
        public void SpawnMail(EntityUid uid, MailTeleporterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
            {
                Logger.Error($"Tried to SpawnMail on {ToPrettyString(uid)} without a valid MailTeleporterComponent!");
                return;
            }

            if (GetUndeliveredParcelCount(uid) >= component.MaximumUndeliveredParcels)
                return;

            _audioSystem.PlayPvs(component.TeleportSound, uid);

            List<(string recipientName, string recipientJob, HashSet<String> accessTags)> candidateList = new();
            foreach (var receiver in EntityQuery<MailReceiverComponent>())
            {
                // Because of the way this works, people are not considered
                // candidates for mail if there is no PDA or ID in their slot
                // or active hand. A better future solution might be checking
                // the station records, possibly cross-referenced with the
                // medical crew scanner to look for living recipients. TODO
                if (_stationSystem.GetOwningStation(receiver.Owner) != _stationSystem.GetOwningStation(uid))
                        continue;
                if (_idCardSystem.TryFindIdCard(receiver.Owner, out var idCard) && TryComp<AccessComponent>(idCard.Owner, out var access)
                    && idCard.FullName != null && idCard.JobTitle != null)
                {
                    HashSet<String> accessTags = access.Tags;
                    var candidateTuple = (idCard.FullName, idCard.JobTitle, accessTags);
                    candidateList.Add(candidateTuple);
                }
            }

            if (candidateList.Count <= 0)
            {
                Logger.Error("List of mail candidates was empty!");
                return;
            }

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>("RandomMailDeliveryPool", out var pool))
            {
                Logger.Error("Can't index the random mail delivery pool!");
                return;
            }

            for (int i = 0;
                i < component.MinimumDeliveriesPerTeleport + candidateList.Count / component.CandidatesPerDelivery;
                i++)
            {
                var mail = EntityManager.SpawnEntity(pool.Pick(), Transform(uid).Coordinates);
                var candidate = _random.Pick(candidateList);
                SetupMail(mail, component, candidate.recipientName, candidate.recipientJob, candidate.accessTags);
            }
        }

        public void OpenMail(EntityUid uid, MailComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _audioSystem.PlayPvs(component.OpenSound, uid);

            if (user != null)
                _handsSystem.TryDrop((EntityUid) user);

            if (!_containerSystem.TryGetContainer(uid, "contents", out var contents))
            {
                Logger.Error($"Mail {ToPrettyString(uid)} was missing contents container!");
                return;
            }

            foreach (var entity in contents.ContainedEntities.ToArray())
            {
                _handsSystem.PickupOrDrop(user, entity);
            }

            _tagSystem.AddTag(uid, "Trash");
            _tagSystem.AddTag(uid, "Recyclable");
            component.Enabled = false;
            UpdateMailTrashState(uid, true);
        }

        private void UpdateAntiTamperVisuals(EntityUid uid, bool isLocked)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsLocked, isLocked);
        }

        private void UpdateMailTrashState(EntityUid uid, bool isTrash)
        {
            _appearanceSystem.SetData(uid, MailVisuals.IsTrash, isTrash);
        }
    }
}
