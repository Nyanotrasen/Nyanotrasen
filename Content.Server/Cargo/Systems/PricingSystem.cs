using System.Linq;
using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Prototypes;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stacks;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// This handles calculating the price of items, and implements two basic methods of pricing materials.
/// </summary>
public sealed partial class PricingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("pricing");

        InitializeSupplyDemand();

        SubscribeLocalEvent<MobPriceComponent, PriceCalculationEvent>(CalculateMobPrice);

        _consoleHost.RegisterCommand("appraisegrid",
            "Calculates the total value of the given grids.",
            "appraisegrid <grid Ids>", AppraiseGridCommand);
    }

    [AdminCommand(AdminFlags.Debug)]
    private void AppraiseGridCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments.");
            return;
        }

        foreach (var gid in args)
        {
            if (!EntityUid.TryParse(gid, out var gridId) || !gridId.IsValid())
            {
                shell.WriteError($"Invalid grid ID \"{gid}\".");
                continue;
            }

            if (!_mapManager.TryGetGrid(gridId, out var mapGrid))
            {
                shell.WriteError($"Grid \"{gridId}\" doesn't exist.");
                continue;
            }

            List<(double, EntityUid)> mostValuable = new();

            var value = AppraiseGrid(mapGrid.Owner, null, (uid, price) =>
            {
                mostValuable.Add((price, uid));
                mostValuable.Sort((i1, i2) => i2.Item1.CompareTo(i1.Item1));
                if (mostValuable.Count > 5)
                    mostValuable.Pop();
            });

            shell.WriteLine($"Grid {gid} appraised to {value} spacebucks.");
            shell.WriteLine($"The top most valuable items were:");
            foreach (var (price, ent) in mostValuable)
            {
                shell.WriteLine($"- {ToPrettyString(ent)} @ {price} spacebucks");
            }
        }
    }

    private void CalculateMobPrice(EntityUid uid, MobPriceComponent component, ref PriceCalculationEvent args)
    {
        // TODO: Estimated pricing.
        if (args.Handled)
            return;

        if (!TryComp<BodyComponent>(uid, out var body) || !TryComp<MobStateComponent>(uid, out var state))
        {
            Logger.ErrorS("pricing", $"Tried to get the mob price of {ToPrettyString(uid)}, which has no {nameof(BodyComponent)} and no {nameof(MobStateComponent)}.");
            return;
        }

        var partList = _bodySystem.GetBodyAllSlots(uid, body).ToList();
        var totalPartsPresent = partList.Sum(x => x.Child != null ? 1 : 0);
        var totalParts = partList.Count;

        var partRatio = totalPartsPresent / (double) totalParts;
        var partPenalty = component.Price * (1 - partRatio) * component.MissingBodyPartPenalty;

        var basePrice = (component.Price - partPenalty) * (_mobStateSystem.IsAlive(uid, state) ? 1.0 : component.DeathPenalty);

        if (body.Prototype != null &&
            _prototypeManager.TryIndex<BodyPrototype>(body.Prototype, out var bodyProto))
        {
            var supply = GetMobSupply(bodyProto);
            var demand = GetMobDemand(bodyProto);

            args.Price += GetSupplyDemandPrice(basePrice, bodyProto.HalfPriceSurplus, supply, demand);

            if (args.Sale)
                AddMobSupply(bodyProto, 1);
        }
        else
        {
            args.Price += basePrice;
        }
    }

    private double GetSolutionPrice(SolutionContainerManagerComponent component, bool sale = false)
    {
        var price = 0.0;

        foreach (var solution in component.Solutions.Values)
        {
            foreach (var reagent in solution.Contents)
            {
                if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.ReagentId, out var reagentProto))
                    continue;

                var supply = GetReagentSupply(reagentProto);
                var demand = GetReagentDemand(reagentProto);

                price += GetSupplyDemandPrice((float) reagent.Quantity * reagentProto.PricePerUnit, reagentProto.HalfPriceSurplus, supply, demand);

                if (sale)
                    AddReagentSupply(reagentProto, reagent.Quantity);
            }
        }

        return price;
    }

    private double GetMaterialPrice(PhysicalCompositionComponent component, bool sale = false)
    {
        double price = 0;
        foreach (var (id, quantity) in component.MaterialComposition)
        {
            var proto = _prototypeManager.Index<MaterialPrototype>(id);

            var supply = GetMaterialSupply(proto);
            var demand = GetMaterialDemand(proto);

            price += GetSupplyDemandPrice(quantity * proto.Price, proto.HalfPriceSurplus, supply, demand);

            if (sale)
                AddMaterialSupply(proto, quantity);
        }
        return price;
    }

    /// <summary>
    /// Get a rough price for an entityprototype. Does not consider contained entities.
    /// </summary>
    public double GetEstimatedPrice(EntityPrototype prototype)
    {
        var ev = new EstimatedPriceCalculationEvent()
        {
            Prototype = prototype,
        };

        RaiseLocalEvent(ref ev);

        if (ev.Handled)
            return ev.Price;

        var price = ev.Price;
        price += GetMaterialsPrice(prototype);
        price += GetSolutionsPrice(prototype);
        // Can't use static price with stackprice
        var oldPrice = price;
        price += GetStackPrice(prototype);

        if (oldPrice.Equals(price))
        {
            price += GetStaticPrice(prototype);
        }

        // TODO: Proper container support.

        return price;
    }

    /// <summary>
    /// Appraises an entity, returning it's price.
    /// </summary>
    /// <param name="uid">The entity to appraise.</param>
    /// <param name="sale">Should this price calculation affect the market?</param>
    /// <returns>The price of the entity.</returns>
    /// <remarks>
    /// This fires off an event to calculate the price.
    /// Calculating the price of an entity that somehow contains itself will likely hang.
    ///
    /// The sale flag exists to simplify informing the supply and demand system
    /// about supply increases.
    /// </remarks>
    public double GetPrice(EntityUid uid, bool sale = false)
    {
        var ev = new PriceCalculationEvent() { Sale = sale };
        RaiseLocalEvent(uid, ref ev);

        if (ev.Handled)
            return ev.Price;

        var price = ev.Price;
        //TODO: Add an OpaqueToAppraisal component or similar for blocking the recursive descent into containers, or preventing material pricing.
        // DO NOT FORGET TO UPDATE ESTIMATED PRICING
        price += GetMaterialsPrice(uid, sale);
        price += GetSolutionsPrice(uid, sale);

        // Can't use static price with stackprice
        var oldPrice = price;
        price += GetStackPrice(uid, sale);

        if (oldPrice.Equals(price))
        {
            price += GetStaticPrice(uid);
        }

        if (TryComp<ContainerManagerComponent>(uid, out var containers))
        {
            foreach (var container in containers.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    price += GetPrice(ent);
                }
            }
        }

        return price;
    }

    private double GetMaterialsPrice(EntityUid uid, bool sale = false)
    {
        double price = 0;

        if (HasComp<MaterialComponent>(uid) &&
            TryComp<PhysicalCompositionComponent>(uid, out var composition))
        {
            var matPrice = GetMaterialPrice(composition, sale);
            if (TryComp<StackComponent>(uid, out var stack))
                matPrice *= stack.Count;

            price += matPrice;
        }

        return price;
    }

    private double GetMaterialsPrice(EntityPrototype prototype)
    {
        double price = 0;

        if (prototype.Components.ContainsKey(_factory.GetComponentName(typeof(MaterialComponent))) &&
            prototype.Components.TryGetValue(_factory.GetComponentName(typeof(PhysicalCompositionComponent)), out var composition))
        {
            var compositionComp = (PhysicalCompositionComponent) composition.Component;
            var matPrice = GetMaterialPrice(compositionComp);

            if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StackComponent)), out var stackProto))
            {
                matPrice *= ((StackComponent) stackProto.Component).Count;
            }

            price += matPrice;
        }

        return price;
    }

    private double GetSolutionsPrice(EntityUid uid, bool sale = false)
    {
        var price = 0.0;

        if (TryComp<SolutionContainerManagerComponent>(uid, out var solComp))
        {
            price += GetSolutionPrice(solComp, sale);
        }

        return price;
    }

    private double GetSolutionsPrice(EntityPrototype prototype, bool sale = false)
    {
        var price = 0.0;

        if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(SolutionContainerManagerComponent)), out var solManager))
        {
            var solComp = (SolutionContainerManagerComponent) solManager.Component;
            price += GetSolutionPrice(solComp, sale);
        }

        return price;
    }

    private double GetStackPrice(EntityUid uid, bool sale = false)
    {
        var price = 0.0;

        if (TryComp<StackPriceComponent>(uid, out var stackPrice) &&
            TryComp<StackComponent>(uid, out var stack) &&
            !HasComp<MaterialComponent>(uid)) // don't double count material prices
        {
            var supply = GetStackSupply(stack);
            var demand = GetStackDemand(stackPrice, stack);

            if (sale)
                AddStackSupply(stack, stack.Count);

            price += GetSupplyDemandPrice(stack.Count * stackPrice.Price, stackPrice.HalfPriceSurplus, supply, demand);
        }

        return price;
    }

    private double GetStackPrice(EntityPrototype prototype, bool sale = false)
    {
        var price = 0.0;

        if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StackPriceComponent)), out var stackpriceProto) &&
            prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StackComponent)), out var stackProto) &&
            !prototype.Components.ContainsKey(_factory.GetComponentName(typeof(MaterialComponent))))
        {
            var stackPrice = (StackPriceComponent) stackpriceProto.Component;
            var stack = (StackComponent) stackProto.Component;
            var supply = GetStackSupply(stack);
            var demand = GetStackDemand(stackPrice, stack);

            if (sale)
                AddStackSupply(stack, stack.Count);

            price += GetSupplyDemandPrice(stack.Count * stackPrice.Price, stackPrice.HalfPriceSurplus, supply, demand);
        }

        return price;
    }

    private double GetStaticPrice(EntityUid uid)
    {
        var price = 0.0;

        if (TryComp<StaticPriceComponent>(uid, out var staticPrice))
        {
            price += staticPrice.Price;
        }

        return price;
    }

    private double GetStaticPrice(EntityPrototype prototype)
    {
        var price = 0.0;

        if (prototype.Components.TryGetValue(_factory.GetComponentName(typeof(StaticPriceComponent)), out var staticProto))
        {
            var staticPrice = (StaticPriceComponent) staticProto.Component;
            price += staticPrice.Price;
        }

        return price;
    }

    /// <summary>
    /// Appraises a grid, this is mainly meant to be used by yarrs.
    /// </summary>
    /// <param name="grid">The grid to appraise.</param>
    /// <param name="predicate">An optional predicate that controls whether or not the entity is counted toward the total.</param>
    /// <param name="afterPredicate">An optional predicate to run after the price has been calculated. Useful for high scores or similar.</param>
    /// <returns>The total value of the grid.</returns>
    public double AppraiseGrid(EntityUid grid, Func<EntityUid, bool>? predicate = null, Action<EntityUid, double>? afterPredicate = null)
    {
        var xform = Transform(grid);
        var price = 0.0;

        foreach (var child in xform.ChildEntities)
        {
            if (predicate is null || predicate(child))
            {
                var subPrice = GetPrice(child);
                price += subPrice;
                afterPredicate?.Invoke(child, subPrice);
            }
        }

        return price;
    }
}

/// <summary>
/// A directed by-ref event fired on an entity when something needs to know it's price. This value is not cached.
/// </summary>
[ByRefEvent]
public record struct PriceCalculationEvent()
{
    /// <summary>
    /// The total price of the entity.
    /// </summary>
    public double Price = 0;

    /// <summary>
    /// Is this event being raised for an item that will be sold?
    /// </summary>
    /// <remarks>
    /// If true, this will signal to relevant systems that they may increase
    /// the supply in the market as a side-effect.
    ///
    /// It's not the most intuitive way to do this, but it's more efficient
    /// than firing a GetPrice event then a second, separate Sale event for
    /// every entity sold. It's not unheard of for players to sell hundreds of
    /// entities at a time. Refactor if needed.
    /// </remarks>
    public bool Sale { get; init; } = false;

    /// <summary>
    /// Whether this event was already handled.
    /// </summary>
    public bool Handled = false;
}

/// <summary>
/// Raised broadcast for an entity prototype to determine its estimated price.
/// </summary>
[ByRefEvent]
public record struct EstimatedPriceCalculationEvent()
{
    public EntityPrototype Prototype;

    /// <summary>
    /// The total price of the entity.
    /// </summary>
    public double Price = 0;

    /// <summary>
    /// Whether this event was already handled.
    /// </summary>
    public bool Handled = false;
}
