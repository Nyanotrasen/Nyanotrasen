using System.Threading;
using Content.Shared.Verbs;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Buckle.Components;
using Content.Server.Buckle.Components;
using Content.Server.Bible.Components;
using Content.Server.Stunnable;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Server.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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

            if (component.RequiresBibleUser && !HasComp<BibleUserComponent>(args.User))
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

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(altarComp.RewardPool, out var pool))
                return;

            QueueDel(args.Target);

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
                MetaData(trap).EntityName = Loc.GetString("soul-entity-name", ("trapped", args.Target));
                MetaData(trap).EntityDescription = Loc.GetString("soul-entity-desc", ("trapped", args.Target));
            }
        }

        private void OnSacrificeCancelled(SacrificeCancelledEvent args)
        {
            if (!TryComp<SacrificialAltarComponent>(args.Altar, out var altarComponent))
                return;

            altarComponent.CancelToken = null;
        }

        public void AttemptSacrifice(EntityUid agent, EntityUid patient, EntityUid altar, SacrificialAltarComponent? component = null)
        {
            if (!Resolve(altar, ref component))
                return;

            if (component.CancelToken != null)
                return;

            _stunSystem.TryParalyze(patient, component.SacrificeTime, true);

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
