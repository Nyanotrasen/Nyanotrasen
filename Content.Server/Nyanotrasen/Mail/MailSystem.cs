using Content.Server.Mail.Components;
using Content.Server.Power.Components;
using Content.Server.Popups;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Destructible;
using Content.Shared.Storage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mail;
using Content.Shared.PDA;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Audio;

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

        // TODO: YAML Serializer won't catch this.
        [ViewVariables(VVAccess.ReadWrite)]
        public readonly IReadOnlyList<string> MailPrototypes = new[]
        {
            "MailBikeHorn",
            "MailJunkFood",
            "MailCosplay",
            "MailMoney",
            "MailCigarettes",
            "MailFigurine",
            "MailBible",
            "MailCrayon",
            "MailPlushie",
            "MailPAI",
            "MailSunglasses",
            "MailBlockGameDIY",
            "MailSpaceVillainDIY",
            "MailBooks",
            "MailNoir",
            "MailHighlander",
            "MailFlashlight",
            "MailKnife",
            "MailCigars",
            "MailKatana"
        };


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

                SpawnMail(mailTeleporter.Owner);
            }
        }

        /// <summary>
        /// Set initial appearance so stuff doesn't break.
        /// </summary>
        private void OnInit(EntityUid uid, MailComponent mail, ComponentInit args)
        {
            UpdateAntiTamperVisuals(uid, true);
            UpdateMailTrashState(uid, false);
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
            /// This needs to be revisited for multistation
            /// For now let's just add the bounty to the first
            /// console we find.
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
            if (component.Enabled)
                OpenMail(uid, component);
            UpdateAntiTamperVisuals(uid, false);
        }

        public void SpawnMail(EntityUid uid)
        {
            SoundSystem.Play("/Audio/Effects/teleport_arrival.ogg", Filter.Pvs(uid), uid);
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

            for (int i = 0; i < ((candidateList.Count / 8) + 1); i++)
            {
                var mail = EntityManager.SpawnEntity(_random.Pick(MailPrototypes), Transform(uid).Coordinates);
                var mailComp = EnsureComp<MailComponent>(mail);
                var candidate = _random.Pick(candidateList);
                mailComp.RecipientJob = candidate.recipientJob;
                mailComp.Recipient = candidate.recipientName;

                var accessReader = EnsureComp<AccessReaderComponent>(mail);
                accessReader.AccessLists.Add(candidate.accessTags);
            }
        }

        public void OpenMail(EntityUid uid, MailComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            SoundSystem.Play("/Audio/Effects/packetrip.ogg", Filter.Pvs(uid), uid);

            var contentList = EntitySpawnCollection.GetSpawns(component.Contents, _random);
            if (user != null)
                _handsSystem.TryDrop((EntityUid) user);
            foreach (var item in contentList)
            {
                var entity = EntityManager.SpawnEntity(item, Transform(uid).Coordinates);
                if (user != null)
                    _handsSystem.PickupOrDrop(user, entity);
            }
            _tagSystem.AddTag(uid, "Trash");
            _tagSystem.AddTag(uid, "Recyclable");
            component.Enabled = false;
            UpdateMailTrashState(uid, true);
        }

        private void UpdateAntiTamperVisuals(EntityUid uid, bool isLocked)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(MailVisuals.IsLocked, isLocked);
        }

        private void UpdateMailTrashState(EntityUid uid, bool isTrash)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(MailVisuals.IsTrash, isTrash);
        }
    }
}
