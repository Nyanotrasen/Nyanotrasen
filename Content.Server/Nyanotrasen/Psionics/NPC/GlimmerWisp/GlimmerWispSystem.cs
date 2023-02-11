using System.Threading;
using Content.Server.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Verbs;
using Content.Shared.Rejuvenate;
using Content.Shared.ActionBlocker;
using Content.Shared.Pulling.Components;
using Content.Server.Popups;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.Carrying;
using Robust.Shared.Player;
using Robust.Server.GameObjects;

namespace Content.Server.Psionics.NPC.GlimmerWisp
{
    public sealed class GlimmerWispSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageable = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly MobStateSystem _mobs = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly NPCCombatTargetSystem _combatTargetSystem = default!;
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


        public bool NPCStartLifedrain(EntityUid uid, EntityUid target, GlimmerWispComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;
            if (!HasComp<PsionicComponent>(target))
                return false;
            if (!_mobs.IsCritical(target))
                return false;
            if (!_actionBlocker.CanInteract(uid, target))
                return false;

            StartLifeDrain(uid, target, component);
            return true;
        }

        public void StartLifeDrain(EntityUid uid, EntityUid target, GlimmerWispComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

           component.CancelToken = new CancellationTokenSource();
           component.DrainTarget = target;
            _popups.PopupEntity(Loc.GetString("life-drain-second-start", ("wisp", uid)), target, target, Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("life-drain-third-start", ("wisp", uid), ("target", target)), target, Filter.PvsExcept(target), true, Shared.Popups.PopupType.LargeCaution);

            component.DrainStingStream = _audioSystem.PlayPvs(component.DrainSoundPath, target);
            _doAfter.DoAfter(new DoAfterEventArgs(uid, component.DrainDelay, component.CancelToken.Token, target: target)
            {
                BroadcastFinishedEvent = new TargetDrainSuccessfulEvent(uid, target),
                BroadcastCancelledEvent = new DrainCancelledEvent(uid, target),
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

            _popups.PopupEntity(Loc.GetString("life-drain-second-end", ("wisp", ev.Drainer)), ev.Target, ev.Target, Shared.Popups.PopupType.LargeCaution);
            _popups.PopupEntity(Loc.GetString("life-drain-third-end", ("wisp", ev.Drainer), ("target", ev.Target)), ev.Target, Filter.PvsExcept(ev.Target), true, Shared.Popups.PopupType.LargeCaution);

            var rejEv = new RejuvenateEvent();
            RaiseLocalEvent(ev.Drainer, rejEv);

            _audioSystem.PlayPvs(component.DrainFinishSoundPath, ev.Drainer);

            DamageSpecifier damage = new();
            damage.DamageDict.Add("Asphyxiation", 200);
            _damageable.TryChangeDamage(ev.Target, damage, true, origin:ev.Drainer);
        }

        private void OnDrainCancelled(DrainCancelledEvent ev)
        {
            if (!TryComp<GlimmerWispComponent>(ev.Drainer, out var component))
                return;

            component.CancelToken = null;
            component.DrainStingStream?.Stop();

            if (TryComp<SharedPullableComponent>(ev.Target, out var pullable) && pullable.Puller != null)
            {
                _combatTargetSystem.StartHostility(ev.Drainer, pullable.Puller.Value);
                return;
            }

            if (TryComp<BeingCarriedComponent>(ev.Target, out var carried))
            {
                _combatTargetSystem.StartHostility(ev.Drainer, carried.Carrier);
                return;
            }
        }

        private sealed class DrainCancelledEvent : EntityEventArgs
        {
            public EntityUid Drainer;
            public EntityUid Target;

            public DrainCancelledEvent(EntityUid drainer, EntityUid target)
            {
                Drainer = drainer;
                Target = target;
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
