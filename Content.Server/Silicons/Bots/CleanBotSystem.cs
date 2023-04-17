using Content.Shared.Mobs.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Silicons;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
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
            SubscribeLocalEvent<CleanBotComponent, CleanBotCleanDoAfterEvent>(OnDoAfter);
        }

        private void PlayerClean(EntityUid uid, CleanBotComponent component, InteractNoHandEvent args)
        {
            if (!HasComp<PuddleComponent>(args.Target))
                return;

            TryStartClean(uid, component, args.Target.Value);
        }

        private void OnDoAfter(EntityUid uid, CleanBotComponent component, DoAfterEvent args)
        {
            component.IsMopping = false;

            if (args.Handled || args.Cancelled || args.Args.Target == null)
                return;

            if (!TryComp<PuddleComponent>(args.Args.Target.Value, out var puddle))
                return;

            if (!_solution.TryGetSolution(args.Args.Target.Value, puddle.SolutionName, out var solution))
                return;

            _audioSystem.PlayPvs(component.CleanSound, args.Args.Target.Value);

            solution.SplitSolution(component.UnitsToClean);

            if (solution.Volume < 1f)
                QueueDel(args.Args.Target.Value);
        }

        public bool TryStartClean(EntityUid performer, CleanBotComponent component, EntityUid target)
        {
            if (component.IsMopping)
                return false;

            if (!_blocker.CanInteract(performer, target))
                return false;

            if (!_interactionSystem.InRangeUnobstructed(performer, target))
                return false;

            component.CleanTarget = target;
            component.IsMopping = true;

            var ev = new CleanBotCleanDoAfterEvent();
            var args = new DoAfterArgs(performer, component.CleanDelay, ev, performer, target: target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = false
            };

            _doAfter.TryStartDoAfter(args);
            return true;
        }
    }
}
