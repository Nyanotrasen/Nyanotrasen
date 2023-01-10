using System.Threading;
using Content.Shared.Verbs;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Body.Components;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Buckle.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Server.Bible.Components;
using Content.Server.Stunnable;
using Content.Server.DoAfter;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Soul;
using Content.Server.Body.Systems;
using Content.Server.Cloning;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Chapel
{
    public sealed class SacrificialAltarSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SacrificialAltarComponent, GetVerbsEvent<AlternativeVerb>>(AddSacrificeVerb);
            SubscribeLocalEvent<SacrificialAltarComponent, BuckleChangeEvent>(OnBuckleChanged);
            SubscribeLocalEvent<SacrificeSuccessfulEvent>(OnSacrificeSuccessful);
            SubscribeLocalEvent<SacrificeCancelledEvent>(OnSacrificeCancelled);
        }

        private void AddSacrificeVerb(EntityUid uid, SacrificialAltarComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || component.CancelToken != null)
                return;

            if (!TryComp<StrapComponent>(uid, out var strap))
                return;

            EntityUid? sacrificee = null;

            foreach (var entity in strap.BuckledEntities) // mm yes I love hashsets which can't be accessed via index
            {
                sacrificee = entity;
            }

            if (sacrificee == null)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    AttemptSacrifice(args.User, sacrificee.Value, uid, component);
                },
                Text = Loc.GetString("altar-sacrifice-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnBuckleChanged(EntityUid uid, SacrificialAltarComponent component, BuckleChangeEvent args)
        {
            if (component.CancelToken != null)
                component.CancelToken.Cancel();
        }

        private void OnSacrificeSuccessful(SacrificeSuccessfulEvent args)
        {
            if (!TryComp<SacrificialAltarComponent>(args.Altar, out var altarComp))
                return;

            // note: we checked this twice in case they could have gone SSD in the doafter time.
            if (!TryComp<ActorComponent>(args.Target, out var actor))
                return;

            altarComp.CancelToken = null;

            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.User):player} sacrificed {ToPrettyString(args.Target):target} on {ToPrettyString(args.Altar):altar}");

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(altarComp.RewardPool, out var pool))
                return;

            var chance = HasComp<BibleUserComponent>(args.User) ? altarComp.RewardPoolChanceBibleUser : altarComp.RewardPoolChance;

            if (_robustRandom.Prob(chance))
                Spawn(pool.Pick(), Transform(args.Altar).Coordinates);

            int i = _robustRandom.Next(altarComp.BluespaceRewardMin, altarComp.BlueSpaceRewardMax);

            while (i > 0)
            {
                Spawn("MaterialBluespace", Transform(args.Altar).Coordinates);
                i--;
            }

            int reduction = _robustRandom.Next(altarComp.GlimmerReductionMin, altarComp.GlimmerReductionMax);
            _glimmerSystem.Glimmer -= reduction;

            if (actor.PlayerSession.ContentData()?.Mind != null)
            {
                var trap = Spawn(altarComp.TrapPrototype, Transform(args.Altar).Coordinates);
                actor.PlayerSession.ContentData()?.Mind?.TransferTo(trap);

                if (TryComp<SoulCrystalComponent>(trap, out var crystalComponent))
                    crystalComponent.TrueName = MetaData(args.Target).EntityName;

                MetaData(trap).EntityName = Loc.GetString("soul-entity-name", ("trapped", args.Target));
                MetaData(trap).EntityDescription = Loc.GetString("soul-entity-desc", ("trapped", args.Target));
            }

            if (TryComp<BodyComponent>(args.Target, out var body))
            {
                _bodySystem.GibBody(args.Target, true, body, false);
            } else
            {
                QueueDel(args.Target);
            }
        }

        private void OnSacrificeCancelled(SacrificeCancelledEvent args)
        {
            if (!TryComp<SacrificialAltarComponent>(args.Altar, out var altarComponent))
                return;

            altarComponent.CancelToken = null;
            altarComponent.SacrificeStingStream?.Stop();
        }

        public void AttemptSacrifice(EntityUid agent, EntityUid patient, EntityUid altar, SacrificialAltarComponent? component = null)
        {
            if (!Resolve(altar, ref component))
                return;

            if (component.CancelToken != null)
                return;

            // can't sacrifice yourself
            if (agent == patient)
            {
                _popups.PopupEntity(Loc.GetString("altar-failure-reason-self"), altar, agent, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            // you need psionic OR bible user
            if (!HasComp<PsionicComponent>(agent) && !HasComp<BibleUserComponent>(agent))
            {
                _popups.PopupEntity(Loc.GetString("altar-failure-reason-user"), altar, agent, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            // and no golems or familiars or whatever should be sacrificing
            if (!HasComp<HumanoidComponent>(agent))
            {
                _popups.PopupEntity(Loc.GetString("altar-failure-reason-user-humanoid"), altar, agent, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (!HasComp<PsionicComponent>(patient))
            {
                _popups.PopupEntity(Loc.GetString("altar-failure-reason-target", ("target", patient)), altar, agent, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (!HasComp<HumanoidComponent>(patient) && !HasComp<MetempsychosisKarmaComponent>(patient))
            {
                _popups.PopupEntity(Loc.GetString("altar-failure-reason-target-humanoid", ("target", patient)), altar, agent, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (!HasComp<ActorComponent>(patient))
            {
                _popups.PopupEntity(Loc.GetString("altar-failure-reason-target-ssd", ("target", patient)), altar, agent, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (HasComp<BibleUserComponent>(agent))
            {
                if (component.StunTime == null || _timing.CurTime > component.StunTime)
                {
                    _stunSystem.TryParalyze(patient, component.SacrificeTime + TimeSpan.FromSeconds(1), true);
                    component.StunTime = _timing.CurTime + component.StunCD;
                }
            }

            _popups.PopupEntity(Loc.GetString("altar-popup", ("user", agent), ("target", patient)), altar, Shared.Popups.PopupType.LargeCaution);

            component.SacrificeStingStream = _audioSystem.PlayPvs(component.SacrificeSoundPath, altar);
            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(agent, (float) component.SacrificeTime.TotalSeconds, component.CancelToken.Token, target: patient)
            {
                BroadcastFinishedEvent = new SacrificeSuccessfulEvent(agent, (EntityUid) patient, altar),
                BroadcastCancelledEvent = new SacrificeCancelledEvent(component.Owner),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }
        private sealed class SacrificeCancelledEvent : EntityEventArgs
        {
            public EntityUid Altar;

            public SacrificeCancelledEvent(EntityUid altar)
            {
                Altar = altar;
            }
        }

        private sealed class SacrificeSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid Target;
            public EntityUid Altar;
            public SacrificeSuccessfulEvent(EntityUid user, EntityUid target, EntityUid altar)
            {
                User = user;
                Target = target;
                Altar = altar;
            }
        }
    }
}
