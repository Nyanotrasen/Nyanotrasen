using Content.Server.Mail.Components;
using Content.Server.Power.Components;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.Player;

namespace Content.Server.Mail
{
    public sealed class MailSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MailTeleporterComponent, ComponentInit>(OnInit);
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

                SpawnMail(mailTeleporter.Owner, mailTeleporter);
            }
        }

        /// <summary>
        /// We're gonna spawn mail right away so the mailmen have something to do.
        /// <summary>
        private void OnInit(EntityUid uid, MailTeleporterComponent component, ComponentInit args)
        {
            SpawnMail(uid, component);
        }

        /// <summary>
        /// Check the ID against the mail's lock
        /// </summary>
        private void OnAfterInteractUsing(EntityUid uid, MailComponent component, AfterInteractUsingEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<IdCardComponent>(args.Used, out var idCard) || !TryComp<AccessReaderComponent>(uid, out var access))
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

            component.Locked = false;
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

            EntityManager.SpawnEntity("Paper", Transform(uid).Coordinates);
        }
    }
}
