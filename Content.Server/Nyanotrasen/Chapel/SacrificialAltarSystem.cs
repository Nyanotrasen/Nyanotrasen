using System.Threading;
using Content.Shared.Verbs;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Buckle.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Server.Buckle.Components;
using Content.Server.Bible.Components;
using Content.Server.Stunnable;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Soul;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Server.GameObjects;

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

            // you need psionic OR bible user
            if (!HasComp<PsionicComponent>(args.User) && !HasComp<BibleUserComponent>(args.User))
                return;

            if (!TryComp<StrapComponent>(uid, out var strap))
                return;

            EntityUid? sacrificee = null;

            foreach (var entity in strap.BuckledEntities) // mm yes I love hashsets which can't be accessed via index
            {
                if (!HasComp<PsionicComponent>(entity))
                    return;

                if (!HasComp<HumanoidComponent>(entity))
                    return;

                sacrificee = entity;
            }

            if (sacrificee == null)
                return;

            _popups.PopupEntity(Loc.GetString("altar-popup", ("user", args.User), ("target", sacrificee)), uid, Filter.Pvs(uid), Shared.Popups.PopupType.LargeCaution);

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

            altarComp.CancelToken = null;

            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.User):player} sacrified {ToPrettyString(args.Target):target} on {ToPrettyString(args.Altar):altar}");

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(altarComp.RewardPool, out var pool))
                return;

            QueueDel(args.Target);
            _audioSystem.PlayPvs(altarComp.FinishSound, args.Altar);

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

            if (!TryComp<ActorComponent>(args.Target, out var actor))
                return;

            if (actor.PlayerSession.ContentData()?.Mind != null)
            {
                var trap = Spawn(altarComp.TrapPrototype, Transform(args.Altar).Coordinates);
                actor.PlayerSession.ContentData()?.Mind?.TransferTo(trap);

                if (TryComp<SoulCrystalComponent>(trap, out var crystalComponent))
                    crystalComponent.TrueName = MetaData(args.Target).EntityName;

                MetaData(trap).EntityName = Loc.GetString("soul-entity-name", ("trapped", args.Target));
                MetaData(trap).EntityDescription = Loc.GetString("soul-entity-desc", ("trapped", args.Target));
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

            _stunSystem.TryParalyze(patient, component.SacrificeTime, true);

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
