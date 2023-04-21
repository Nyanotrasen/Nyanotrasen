using System.Threading;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Body.Components;
using Content.Shared.Chapel;
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
            SubscribeLocalEvent<SacrificialAltarComponent, SacrificeDoAfterEvent>(OnDoAfter);
        }

        private void AddSacrificeVerb(EntityUid uid, SacrificialAltarComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || component.DoAfter != null)
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
            if (component.DoAfter != null)
            {
                _doAfterSystem.Cancel(component.DoAfter);
                component.DoAfter = null;
            }
        }

        private void OnDoAfter(EntityUid uid, SacrificialAltarComponent component, SacrificeDoAfterEvent args)
        {
            component.SacrificeStingStream?.Stop();
            component.DoAfter = null;

            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            // note: we checked this twice in case they could have gone SSD in the doafter time.
            if (!TryComp<ActorComponent>(args.Args.Target.Value, out var actor))
                return;

            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(args.Args.User):player} sacrificed {ToPrettyString(args.Args.Target.Value):target} on {ToPrettyString(uid):altar}");

            if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(component.RewardPool, out var pool))
                return;

            var chance = HasComp<BibleUserComponent>(args.Args.User) ? component.RewardPoolChanceBibleUser : component.RewardPoolChance;

            if (_robustRandom.Prob(chance))
                Spawn(pool.Pick(), Transform(uid).Coordinates);

            int i = _robustRandom.Next(component.BluespaceRewardMin, component.BlueSpaceRewardMax);

            while (i > 0)
            {
                Spawn("MaterialBluespace1", Transform(uid).Coordinates);
                i--;
            }

            int reduction = _robustRandom.Next(component.GlimmerReductionMin, component.GlimmerReductionMax);
            _glimmerSystem.Glimmer -= reduction;

            if (actor.PlayerSession.ContentData()?.Mind != null)
            {
                var trap = Spawn(component.TrapPrototype, Transform(uid).Coordinates);
                actor.PlayerSession.ContentData()?.Mind?.TransferTo(trap);

                if (TryComp<SoulCrystalComponent>(trap, out var crystalComponent))
                    crystalComponent.TrueName = MetaData(args.Args.Target.Value).EntityName;

                MetaData(trap).EntityName = Loc.GetString("soul-entity-name", ("trapped", args.Args.Target));
                MetaData(trap).EntityDescription = Loc.GetString("soul-entity-desc", ("trapped", args.Args.Target));
            }

            if (TryComp<BodyComponent>(args.Args.Target, out var body))
            {
                _bodySystem.GibBody(args.Args.Target, true, body, false);
            } else
            {
                QueueDel(args.Args.Target.Value);
            }
        }

        public void AttemptSacrifice(EntityUid agent, EntityUid patient, EntityUid altar, SacrificialAltarComponent? component = null)
        {
            if (!Resolve(altar, ref component))
                return;

            if (component.DoAfter != null)
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
            if (!HasComp<HumanoidAppearanceComponent>(agent))
            {
                _popups.PopupEntity(Loc.GetString("altar-failure-reason-user-humanoid"), altar, agent, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (!HasComp<PsionicComponent>(patient))
            {
                _popups.PopupEntity(Loc.GetString("altar-failure-reason-target", ("target", patient)), altar, agent, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            if (!HasComp<HumanoidAppearanceComponent>(patient) && !HasComp<MetempsychosisKarmaComponent>(patient))
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

            var ev = new SacrificeDoAfterEvent();
            var args = new DoAfterArgs(agent, (float) component.SacrificeTime.TotalSeconds, ev, altar, target: patient, used: altar)
            {
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            };

            _doAfterSystem.TryStartDoAfter(args, out var doAfterId);
            component.DoAfter = doAfterId;
        }
    }
}
