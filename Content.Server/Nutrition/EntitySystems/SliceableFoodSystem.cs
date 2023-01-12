using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Nutrition.EntitySystems
{
    internal sealed class SliceableFoodSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SliceableFoodComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<SliceableFoodComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<SliceableFoodComponent, ComponentStartup>(OnComponentStartup);
        }

        private void OnInteractUsing(EntityUid uid, SliceableFoodComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (TrySliceFood(uid, args.User, args.Used, component))
                args.Handled = true;
        }

        private bool TrySliceFood(EntityUid uid, EntityUid user, EntityUid usedItem,
            SliceableFoodComponent? component = null, FoodComponent? food = null, TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref component, ref food, ref transform) ||
                string.IsNullOrEmpty(component.Slice))
            {
                return false;
            }

            if (!_solutionContainerSystem.TryGetSolution(uid, food.SolutionName, out var solution))
            {
                return false;
            }

            if (!EntityManager.TryGetComponent(usedItem, out UtensilComponent ? utensil) || (utensil.Types & UtensilType.Knife) == 0)
            {
                return false;
            }

            var attemptEvent = new SliceFoodAttemptEvent(user, usedItem, uid);
            RaiseLocalEvent(uid, attemptEvent);
            if (attemptEvent.Cancelled)
                return false;

            var sliceUid = EntityManager.SpawnEntity(component.Slice, transform.Coordinates);

            var lostSolution = _solutionContainerSystem.SplitSolution(uid, solution,
                solution.Volume / FixedPoint2.New(component.Count));

            // Fill new slice
            FillSlice(sliceUid, lostSolution);

            var inCont = _containerSystem.IsEntityInContainer(component.Owner);
            if (inCont)
            {
                _handsSystem.PickupOrDrop(user, sliceUid);
            }
            else
            {
                var xform = Transform(sliceUid);
                _containerSystem.AttachParentToContainerOrGrid(xform);
                xform.LocalRotation = 0;
            }

            _audioSystem.PlayPvs(component.Sound, uid, AudioParams.Default.WithVolume(-2f));

            component.Count--;

            var sliceEvent = new SliceFoodEvent(user, usedItem, uid, sliceUid);
            RaiseLocalEvent(uid, sliceEvent);

            // If someone makes food proto with 1 slice...
            if (component.Count < 1)
            {
                EntityManager.DeleteEntity(uid);
                return true;
            }

            // Split last slice
            if (component.Count > 1)
                return true;

            sliceUid = EntityManager.SpawnEntity(component.Slice, transform.Coordinates);

            // Fill last slice with the rest of the solution
            FillSlice(sliceUid, solution);

            if (inCont)
            {
                _handsSystem.PickupOrDrop(user, sliceUid);
            }
            else
            {
                var xform = Transform(sliceUid);
                _containerSystem.AttachParentToContainerOrGrid(xform);
                xform.LocalRotation = 0;
            }

            var sliceSplitEvent = new SliceFoodEvent(user, usedItem, uid, sliceUid);
            RaiseLocalEvent(uid, sliceSplitEvent);

            EntityManager.DeleteEntity(uid);
            return true;
        }

        private void FillSlice(EntityUid sliceUid, Solution solution)
        {
            // Replace all reagents on prototype not just copying poisons (example: slices of eaten pizza should have less nutrition)
            if (EntityManager.TryGetComponent<FoodComponent>(sliceUid, out var sliceFoodComp) &&
                _solutionContainerSystem.TryGetSolution(sliceUid, sliceFoodComp.SolutionName, out var itsSolution))
            {
                _solutionContainerSystem.RemoveAllSolution(sliceUid, itsSolution);

                var lostSolutionPart = solution.SplitSolution(itsSolution.AvailableVolume);
                _solutionContainerSystem.TryAddSolution(sliceUid, itsSolution, lostSolutionPart);
            }
        }

        private void OnComponentStartup(EntityUid uid, SliceableFoodComponent component, ComponentStartup args)
        {
            component.Count = component.TotalCount;
            var foodComp = EntityManager.EnsureComponent<FoodComponent>(uid);

            EntityManager.EnsureComponent<SolutionContainerManagerComponent>(uid);
            _solutionContainerSystem.EnsureSolution(uid, foodComp.SolutionName);
        }

        private void OnExamined(EntityUid uid, SliceableFoodComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString("sliceable-food-component-on-examine-remaining-slices-text", ("remainingCount", component.Count)));
        }
    }

    public sealed class SliceFoodAttemptEvent : CancellableEntityEventArgs
    {
        /// <summary>
        /// Who's doing the slicing?
        /// <summary>
        public EntityUid User;

        /// <summary>
        /// What's doing the slicing?
        /// <summary>
        public EntityUid Tool;

        /// <summary>
        /// What's being sliced?
        /// <summary>
        public EntityUid Food;

        public SliceFoodAttemptEvent(EntityUid user, EntityUid tool, EntityUid food)
        {
            User = user;
            Tool = tool;
            Food = food;
        }
    }

    public sealed class SliceFoodEvent : EntityEventArgs
    {
        /// <summary>
        /// Who did the slicing?
        /// <summary>
        public EntityUid User;

        /// <summary>
        /// What did the slicing?
        /// <summary>
        public EntityUid Tool;

        /// <summary>
        /// What has been sliced?
        /// <summary>
        /// <remarks>
        /// This could soon be deleted if there was not enough food left to
        /// continue slicing.
        /// </remarks>
        public EntityUid Food;

        /// <summary>
        /// What is the slice?
        /// <summary>
        public EntityUid Slice;

        public SliceFoodEvent(EntityUid user, EntityUid tool, EntityUid food, EntityUid slice)
        {
            User = user;
            Tool = tool;
            Food = food;
            Slice = slice;
        }
    }
}
