using System.Threading;
using Content.Server.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Verbs;
using Content.Shared.Rejuvenate;

namespace Content.Server.Psionics.NPC.GlimmerWisp
{
    public sealed class GlimmerWispSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly MobStateSystem _mobs = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GlimmerWispComponent, GetVerbsEvent<InnateVerb>>(AddDrainVerb);
            SubscribeLocalEvent<TargetDrainSuccessfulEvent>(OnDrainSuccessful);
            SubscribeLocalEvent<DrainCancelledEvent>(OnDrainCancelled);
        }

        private void AddDrainVerb(EntityUid uid, GlimmerWispComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (args.User == args.Target)
                return;
            if (!args.CanAccess)
                return;
            if (!HasComp<PsionicComponent>(args.Target))
                return;
            if (!_mobs.IsCritical(args.Target))
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StartLifeDrain(uid, args.Target, component);
                },
                Text = Loc.GetString("verb-life-drain"),
                IconTexture = "/Textures/Nyanotrasen/Icons/verbiconfangs.png",
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        public void StartLifeDrain(EntityUid uid, EntityUid target, GlimmerWispComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;


           component.CancelToken = new CancellationTokenSource();
            _doAfter.DoAfter(new DoAfterEventArgs(uid, component.DrainDelay, component.CancelToken.Token, target: target)
            {
                BroadcastFinishedEvent = new TargetDrainSuccessfulEvent(uid, target),
                BroadcastCancelledEvent = new DrainCancelledEvent(uid),
                BreakOnTargetMove = true,
                BreakOnUserMove = false,
                DistanceThreshold = 2f,
                BreakOnStun = true,
                NeedHand = false
            });
        }

        private void OnDrainSuccessful(TargetDrainSuccessfulEvent ev)
        {
            if (!TryComp<GlimmerWispComponent>(ev.Drainer, out var component))
                return;

            component.CancelToken = null;

            var rejEv = new RejuvenateEvent();
            RaiseLocalEvent(ev.Drainer, rejEv);

            DamageSpecifier damage = new();
            damage.DamageDict.Add("Asphyxiation", 200);
            _damageable.TryChangeDamage(ev.Target, damage, true, origin:ev.Drainer);
        }

        private void OnDrainCancelled(DrainCancelledEvent ev)
        {
            if (!TryComp<GlimmerWispComponent>(ev.Drainer, out var component))
                return;

            component.CancelToken = null;
        }

        private sealed class DrainCancelledEvent : EntityEventArgs
        {
            public EntityUid Drainer;

            public DrainCancelledEvent(EntityUid drainer)
            {
                Drainer = drainer;
            }
        }

        private sealed class TargetDrainSuccessfulEvent : EntityEventArgs
        {
            public EntityUid Drainer;
            public EntityUid Target;
            public TargetDrainSuccessfulEvent(EntityUid drainer, EntityUid target)
            {
                Drainer = drainer;
                Target = target;
            }
        }
    }
}
