using System.Diagnostics.CodeAnalysis;
using System.Linq;
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


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MailComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<MailComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<MailComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<MailComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<MailComponent, DestructionEventArgs>(OnDestruction);
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

        /// <summary>
        /// Set initial appearance so stuff doesn't break.
        /// </summary>
        private void OnInit(EntityUid uid, MailComponent mail, ComponentInit args)
        {
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
            _popupSystem.PopupEntity(Loc.GetString("mail-bounty", ("bounty", component.Bounty)), uid, Filter.Entities(args.User));
            component.Locked = false;
            UpdateAntiTamperVisuals(uid, false);

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
        }

        private void OnDestruction(EntityUid uid, MailComponent component, DestructionEventArgs args)
        {
            if (component.Locked)
            {
                _chatSystem.TrySendInGameICMessage(uid, Loc.GetString("mail-penalty", ("credits", component.Penalty)), InGameICChatType.Speak, false);
                _audioSystem.PlayPvs(component.PenaltySound, uid);
                foreach (var account in EntityQuery<StationBankAccountComponent>())
                {
                    if (_stationSystem.GetOwningStation(account.Owner) != _stationSystem.GetOwningStation(uid))
                            continue;

                    _cargoSystem.UpdateBankAccount(account, component.Penalty);
                }
            }

            if (component.Enabled)
                OpenMail(uid, component);
            UpdateAntiTamperVisuals(uid, false);
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

            // Fallback: It breaks or is destroyed in less than 10 damage.
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

        public void SpawnMail(EntityUid uid, MailTeleporterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
            {
                Logger.Error($"Tried to SpawnMail on {ToPrettyString(uid)} without a valid MailTeleporterComponent!");
                return;
            }

            _audioSystem.PlayPvs(component.TeleportSound, uid);

            List<(string recipientName, string recipientJob, HashSet<String> accessTags)> candidateList = new();
            foreach (var receiver in EntityQuery<MailReceiverComponent>())
            {
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
                var mailComp = EnsureComp<MailComponent>(mail);
                var container = _containerSystem.EnsureContainer<Container>(mail, "contents", out var contents);
                var isFragile = false;
                foreach (var item in EntitySpawnCollection.GetSpawns(mailComp.Contents, _random))
                {
                    var entity = EntityManager.SpawnEntity(item, Transform(uid).Coordinates);
                    if (!container.Insert(entity))
                    {
                        Logger.Error($"Can't insert {ToPrettyString(entity)} into new mail delivery {ToPrettyString(mail)}! Deleting it.");
                        QueueDel(entity);
                    }
                    else if (!isFragile && IsEntityFragile(entity, component.FragileDamageThreshold))
                    {
                        isFragile = true;
                        _appearanceSystem.SetData(mail, MailVisuals.IsFragile, true);

                        Logger.Debug($"Spawned a fragile entity {ToPrettyString(entity)} for mail {mail}");
                    }
                }
                var candidate = _random.Pick(candidateList);
                mailComp.RecipientJob = candidate.recipientJob;
                mailComp.Recipient = candidate.recipientName;

                if (TryMatchJobTitleToIcon(candidate.recipientJob, out string? icon))
                    _appearanceSystem.SetData(mail, MailVisuals.JobIcon, icon);

                var accessReader = EnsureComp<AccessReaderComponent>(mail);
                accessReader.AccessLists.Add(candidate.accessTags);
            }
        }

        public void OpenMail(EntityUid uid, MailComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            _audioSystem.PlayPvs(component.OpenSound, uid);

            if (user != null)
                _handsSystem.TryDrop((EntityUid) user);
            foreach (var entity in _containerSystem.GetContainer(uid, "contents").ContainedEntities.ToArray())
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
