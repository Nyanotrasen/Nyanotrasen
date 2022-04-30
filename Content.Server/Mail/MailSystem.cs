using Content.Server.Mail.Components;
using Content.Server.Power.Components;

namespace Content.Server.Mail
{
    public sealed class MailSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MailTeleporterComponent, ComponentInit>(OnInit);
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

        public void SpawnMail(EntityUid uid, MailTeleporterComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            EntityManager.SpawnEntity("Paper", Transform(uid).Coordinates);
        }
    }
}
