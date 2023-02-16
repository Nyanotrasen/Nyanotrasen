using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.NPC.Components;
using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
using Robust.Shared.Physics.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Silicons.Bots
{
    public sealed class CleanBotSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobs = default!;
        [Dependency] private readonly SolutionContainerSystem _solution = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CleanBotComponent, InteractNoHandEvent>(PlayerClean);
            SubscribeLocalEvent<TargetCleanSuccessfulEvent>(OnCleanSuccessful);
            SubscribeLocalEvent<CleanCancelledEvent>(OnCleanCancelled);
        }

        private void PlayerClean(EntityUid uid, CleanBotComponent component, InteractNoHandEvent args)
        {
            if (!HasComp<PuddleComponent>(args.Target))
                return;

            TryStartClean(uid, component, args.Target.Value);
        }

        public bool TryStartClean(EntityUid performer, CleanBotComponent component, EntityUid target)
        {
            if (component.CancelToken != null)
                return false;

            if (!_blocker.CanInteract(performer, target))
                return false;

            if (!_interactionSystem.InRangeUnobstructed(performer, target))
                return false;

            component.CancelToken = new CancellationTokenSource();
            component.CleanTarget = target;

            _doAfter.DoAfter(new DoAfterEventArgs(performer, component.CleanDelay, component.CancelToken.Token, target: target)
            {
                BroadcastFinishedEvent = new TargetCleanSuccessfulEvent(performer, target),
                BroadcastCancelledEvent = new CleanCancelledEvent(performer),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = false
            });

            return true;
        }

        private void OnCleanSuccessful(TargetCleanSuccessfulEvent ev)
        {
            if (!TryComp<CleanBotComponent>(ev.Cleaner, out var cleanbot))
                return;

            cleanbot.CancelToken = null;

            if (!TryComp<PuddleComponent>(ev.Target, out var puddle))
                return;

            if (!_solution.TryGetSolution(ev.Target, puddle.SolutionName, out var solution))
                return;

            _audioSystem.PlayPvs(cleanbot.CleanSound, ev.Target);

            solution.SplitSolution(cleanbot.UnitsToClean);

            if (solution.Volume < 1f)
                QueueDel(ev.Target);
        }

        private void OnCleanCancelled(CleanCancelledEvent ev)
        {
            if (!TryComp<CleanBotComponent>(ev.Cleaner, out var cleanbot))
                return;

            cleanbot.CancelToken = null;
        }
        private sealed class CleanCancelledEvent : EntityEventArgs
        {
            public EntityUid Cleaner;
            public CleanCancelledEvent(EntityUid cleaner)
            {
                Cleaner = cleaner;
            }
        }

        private sealed class TargetCleanSuccessfulEvent : EntityEventArgs
        {
            public EntityUid Cleaner;
            public EntityUid Target;
            public TargetCleanSuccessfulEvent(EntityUid cleaner, EntityUid target)
            {
                Cleaner = cleaner;
                Target = target;
            }
        }
    }
}
