using Content.Server.Mail.Components;
using Content.Server.Power.Components;
using Content.Server.Popups;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Storage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mail;
using Content.Shared.PDA;
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
            "MailCapGun",
            "MailGatfruit",
            "MailCrayon",
            "MailPlushie",
            "MailPAI",
            "MailSunglasses",
            "MailKatana"
        };


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MailComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<MailComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<MailComponent, ExaminedEvent>(OnExamined);
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


                SoundSystem.Play(Filter.Pvs(mailTeleporter.Owner), "/Audio/Effects/teleport_arrival.ogg", mailTeleporter.Owner);
                SpawnMail(mailTeleporter.Owner, mailTeleporter);
            }
        }

        /// <summary>
        /// Try to open the mail.
        /// <summary>
        private void OnUseInHand(EntityUid uid, MailComponent component, UseInHandEvent args)
        {
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

            if (!_accessSystem.IsAllowed(access, args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("mail-invalid-access"), uid, Filter.Entities(args.User));
                return;
            }
            _popupSystem.PopupEntity(Loc.GetString("mail-bounty", ("bounty", component.Bounty)), uid, Filter.Entities(args.User));
            component.Locked = false;
            UpdateMailVisuals(uid, false);
            /// This needs to be revisited for multistation
            /// For now let's just add the bounty to the first
            /// console we find.
            foreach (var console in EntityQuery<CargoConsoleComponent>())
            {
                if (console.BankAccount != null)
                    console.BankAccount.Balance += component.Bounty;
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

        public void SpawnMail(EntityUid uid, MailTeleporterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;
            /// This needs to be revisited for multistation
            List<(string recipientName, string recipientJob, AccessComponent access)> candidateList = new();
            foreach (var receiver in EntityQuery<MailReceiverComponent>())
            {
                if (_idCardSystem.TryFindIdCard(receiver.Owner, out var idCard) && TryComp<AccessComponent>(idCard.Owner, out var access)
                    && idCard.FullName != null && idCard.JobTitle != null)
                {
                    var candidateTuple = (idCard.FullName, idCard.JobTitle, access);
                    candidateList.Add(candidateTuple);
                }
            }

            if (candidateList.Count <= 0)
            {
                Logger.Error("List of mail candidates was empty!");
                return;
            }


            for (int i = (candidateList.Count / 8) + 1; i < 3; i++)
            {
                var mail = EntityManager.SpawnEntity(_random.Pick(MailPrototypes), Transform(uid).Coordinates);
                var mailComp = EnsureComp<MailComponent>(mail);
                var candidate = _random.Pick(candidateList);
                mailComp.RecipientJob = candidate.recipientJob;
                mailComp.Recipient = candidate.recipientName;

                var accessReader = EnsureComp<AccessReaderComponent>(mail);
                accessReader.AccessLists.Add(candidate.access.Tags);
            }
        }

        public void OpenMail(EntityUid uid, MailComponent? component = null, EntityUid? user = null)
        {
            if (!Resolve(uid, ref component))
                return;

            SoundSystem.Play(Filter.Pvs(uid), "/Audio/Effects/packetrip.ogg", uid);

            var contentList = EntitySpawnCollection.GetSpawns(component.Contents, _random);

            foreach (var item in contentList)
            {
                var entity = EntityManager.SpawnEntity(item, Transform(uid).Coordinates);
                if (user != null)
                    _handsSystem.PickupOrDrop(user, entity);
            }
            EntityManager.QueueDeleteEntity(uid);
        }

        private void UpdateMailVisuals(EntityUid uid, bool isLocked)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            appearance.SetData(MailVisuals.IsLocked, isLocked);
        }
    }
}
