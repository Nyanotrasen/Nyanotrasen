using System.Threading.Tasks;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Chemistry;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    ///     Gives click behavior for transferring to/from other reagent containers.
    /// </summary>
    [RegisterComponent]
    public sealed class SolutionTransferComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        // Behavior is as such:
        // If it's a reagent tank, TAKE reagent.
        // If it's anything else, GIVE reagent.
        // Of course, only if possible.

        /// <summary>
        ///     The amount of solution to be transferred from this solution when clicking on other solutions with it.
        /// </summary>
        [DataField("transferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 TransferAmount { get; set; } = FixedPoint2.New(5);

        /// <summary>
        ///     The minimum amount of solution that can be transferred at once from this solution.
        /// </summary>
        [DataField("minTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MinimumTransferAmount { get; set; } = FixedPoint2.New(5);

        /// <summary>
        ///     The maximum amount of solution that can be transferred at once from this solution.
        /// </summary>
        [DataField("maxTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MaximumTransferAmount { get; set; } = FixedPoint2.New(50);

        /// <summary>
        ///     Can this entity take reagent from reagent tanks?
        /// </summary>
        [DataField("canReceive")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanReceive { get; set; } = true;

        /// <summary>
        ///     Can this entity give reagent to other reagent containers?
        /// </summary>
        [DataField("canSend")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanSend { get; set; } = true;

        /// <summary>
        /// Whether you're allowed to change the transfer amount.
        /// </summary>
        [DataField("canChangeTransferAmount")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanChangeTransferAmount { get; set; } = false;

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(TransferAmountUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        public void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            if (serverMsg.Session.AttachedEntity == null)
                return;

            switch (serverMsg.Message)
            {
                case TransferAmountSetValueMessage svm:
                    var sval = svm.Value.Float();
                    var amount = Math.Clamp(sval, MinimumTransferAmount.Float(),
                        MaximumTransferAmount.Float());

                    serverMsg.Session.AttachedEntity.Value.PopupMessage(Loc.GetString("comp-solution-transfer-set-amount",
                        ("amount", amount)));
                    SetTransferAmount(FixedPoint2.New(amount));
                    break;
            }
        }

        public void SetTransferAmount(FixedPoint2 amount)
        {
            amount = FixedPoint2.New(Math.Clamp(amount.Int(), MinimumTransferAmount.Int(),
                MaximumTransferAmount.Int()));
            TransferAmount = amount;
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach || eventArgs.Target == null)
                return false;

            var target = eventArgs.Target!.Value;
            var solutionsSys = EntitySystem.Get<SolutionContainerSystem>();
            var transferSystem = EntitySystem.Get<SolutionTransferSystem>();

            //Special case for reagent tanks, because normally clicking another container will give solution, not take it.
            if (CanReceive  && !_entities.HasComponent<RefillableSolutionComponent>(target) // target must not be refillable (e.g. Reagent Tanks)
                            && solutionsSys.TryGetDrainableSolution(target, out var targetDrain) // target must be drainable
                            && _entities.TryGetComponent(Owner, out RefillableSolutionComponent refillComp)
                            && solutionsSys.TryGetRefillableSolution(Owner, out var ownerRefill, refillable: refillComp))

            {

                var transferAmount = TransferAmount; // This is the player-configurable transfer amount of "Owner," not the target reagent tank.

                if (_entities.TryGetComponent(Owner, out RefillableSolutionComponent? refill) && refill.MaxRefill != null) // Owner is the entity receiving solution from target.
                {
                    transferAmount = FixedPoint2.Min(transferAmount, (FixedPoint2) refill.MaxRefill); // if the receiver has a smaller transfer limit, use that instead
                }

                var transferred = transferSystem.Transfer(eventArgs.User, target, targetDrain, Owner, ownerRefill, transferAmount);
                if (transferred > 0)
                {
                    var toTheBrim = ownerRefill.AvailableVolume == 0;
                    var msg = toTheBrim
                        ? "comp-solution-transfer-fill-fully"
                        : "comp-solution-transfer-fill-normal";

                    target.PopupMessage(eventArgs.User,
                        Loc.GetString(msg, ("owner", eventArgs.Target), ("amount", transferred), ("target", Owner)));
                    return true;
                }
            }

            // if target is refillable, and owner is drainable
            if (CanSend && solutionsSys.TryGetRefillableSolution(target, out var targetRefill)
                        && solutionsSys.TryGetDrainableSolution(Owner, out var ownerDrain))
            {
                var transferAmount = TransferAmount;

                if (_entities.TryGetComponent(target, out RefillableSolutionComponent? refill) && refill.MaxRefill != null)
                {
                    transferAmount = FixedPoint2.Min(transferAmount, (FixedPoint2) refill.MaxRefill);
                }

                var transferred = transferSystem.Transfer(eventArgs.User, Owner, ownerDrain, target, targetRefill, transferAmount);

                if (transferred > 0)
                {
                    Owner.PopupMessage(eventArgs.User,
                        Loc.GetString("comp-solution-transfer-transfer-solution",
                            ("amount", transferred),
                            ("target", target)));

                    return true;
                }
            }

            return false;
        }
    }
}
