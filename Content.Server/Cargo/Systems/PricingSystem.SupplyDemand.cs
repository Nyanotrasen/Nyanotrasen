using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Cargo.Components;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Stacks;

namespace Content.Server.Cargo.Systems
{
    public sealed partial class PricingSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        // The galactic supply & demand rates.
        private Dictionary<string, int> _supplyEntity = new();
        private Dictionary<string, int> _demandEntity = new();

        private Dictionary<string, float> _supplyGas = new();
        private Dictionary<string, float> _demandGas = new();

        private Dictionary<string, int> _supplyStack = new();
        private Dictionary<string, int> _demandStack = new();

        private Dictionary<string, FixedPoint2> _supplyReagent = new();
        private Dictionary<string, FixedPoint2> _demandReagent = new();


        private TimeSpan _updateInterval = TimeSpan.FromMinutes(1);
        private TimeSpan _nextUpdate = TimeSpan.Zero;
        private float _supplyReductionChance = 0.33f;


        public void InitializeSupplyDemand()
        {
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
            SubscribeLocalEvent<DynamicPriceComponent, PriceCalculationEvent>(CalculateDynamicPrice);
        }

        private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
        {
            _supplyEntity.Clear();
            _demandEntity.Clear();
            _supplyGas.Clear();
            _demandGas.Clear();
            _supplyStack.Clear();
            _demandStack.Clear();
            _supplyReagent.Clear();
            _demandReagent.Clear();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_gameTiming.CurTime < _nextUpdate)
                return;

            // Randomly reduce the market's entity supply.
            foreach (var (key, units) in _supplyEntity)
            {
                if (_random.Prob(_supplyReductionChance))
                    _supplyEntity[key] = Math.Max(0, units - 1);
            }

            // Randomly reduce the market's stack supply.
            foreach (var (key, units) in _supplyStack)
            {
                if (_random.Prob(_supplyReductionChance))
                    _supplyStack[key] = Math.Max(0, units - 10);
            }

            // Randomly reduce the market's reagent supply.
            var fixedReduction = FixedPoint2.New(20f);
            foreach (var (key, units) in _supplyReagent)
            {
                if (_random.Prob(_supplyReductionChance))
                    _supplyReagent[key] = FixedPoint2.Max(FixedPoint2.Zero, units - fixedReduction);
            }

            // Randomly reduce the market's gas supply.
            foreach (var (key, units) in _supplyGas)
            {
                if (_random.Prob(_supplyReductionChance))
                    _supplyGas[key] = Math.Max(0f, units - 100f);
            }

            _nextUpdate = _gameTiming.CurTime + _updateInterval;
        }

        public double GetSupplyDemandPrice(double basePrice, float halfPriceSurplus, float supply, float demand)
        {
            return basePrice *
                demand /
                Math.Max(1f, halfPriceSurplus + supply);
        }

        public double GetSupplyDemandPrice(double basePrice, int halfPriceSurplus, int supply, int demand)
        {
            return basePrice *
                (float) demand /
                (float) Math.Max(1f, halfPriceSurplus + supply);
        }

        public double GetSupplyDemandPrice(double basePrice, FixedPoint2 halfPriceSurplus, FixedPoint2 supply, FixedPoint2 demand)
        {
            return basePrice *
                (float) demand /
                (float) Math.Max(1f, (float) (halfPriceSurplus + supply));
        }

        /// <summary>
        /// Return a string identifier for an entity in the marketplace.
        /// </summary>
        public string GetEntityCommodityId(EntityUid uid, DynamicPriceComponent component)
        {
            if (component.CommodityId != null)
                return component.CommodityId;

            var proto = MetaData(uid).EntityPrototype?.ID;
            if (proto != null)
                return proto;

            return "UnknownCommodity"; // Who knows.
        }

        public void AddEntitySupply(EntityUid uid, DynamicPriceComponent component, int units)
        {
            var id = GetEntityCommodityId(uid, component);

            if (_supplyEntity.ContainsKey(id))
                _supplyEntity[id] = Math.Max(0, _supplyEntity[id] + units);
            else
                _supplyEntity[id] = Math.Max(0, units);
        }

        public int GetEntitySupply(EntityUid uid, DynamicPriceComponent component)
        {
            var id = GetEntityCommodityId(uid, component);

            if (!_supplyEntity.ContainsKey(id))
                return 0;

            return _supplyEntity[id];
        }

        public int GetEntityDemand(EntityUid uid, DynamicPriceComponent component)
        {
            var id = GetEntityCommodityId(uid, component);

            if (!_demandEntity.ContainsKey(id))
                return component.HalfPriceSurplus;

            return _demandEntity[id];
        }

        private void CalculateDynamicPrice(EntityUid uid, DynamicPriceComponent component, ref PriceCalculationEvent args)
        {
            var id = GetEntityCommodityId(uid, component);

            var supply = GetEntitySupply(uid, component);
            var demand = GetEntityDemand(uid, component);

            args.Price += GetSupplyDemandPrice(component.Price, component.HalfPriceSurplus, supply, demand);

            if (args.Sale)
                AddEntitySupply(uid, component, 1);
        }

        public void AddGasSupply(GasPrototype gas, float moles)
        {
            if (_supplyGas.ContainsKey(gas.ID))
                _supplyGas[gas.ID] = Math.Max(0f, _supplyGas[gas.ID] + moles);
            else
                _supplyGas[gas.ID] = Math.Max(0f, moles);
        }

        public float GetGasSupply(GasPrototype gas)
        {
            if (!_supplyGas.ContainsKey(gas.ID))
                return 0f;

            return _supplyGas[gas.ID];
        }

        public float GetGasDemand(GasPrototype gas)
        {
            if (!_demandGas.ContainsKey(gas.ID))
                return gas.HalfPriceSurplus;

            return _demandGas[gas.ID];
        }

        public void AddStackSupply(StackComponent stack, int units)
        {
            if (stack.StackTypeId == null)
            {
                _sawmill.Error($"AddStackSupply: StackTypeId is null for {ToPrettyString(stack.Owner)}.");
                return;
            }

            if (_supplyStack.ContainsKey(stack.StackTypeId))
                _supplyStack[stack.StackTypeId] = Math.Max(0, _supplyStack[stack.StackTypeId] + units);
            else
                _supplyStack[stack.StackTypeId] = Math.Max(0, units);
        }

        public int GetStackSupply(StackComponent stack)
        {
            if (stack.StackTypeId == null)
            {
                _sawmill.Error($"GetStackSupply: StackTypeId is null for {ToPrettyString(stack.Owner)}.");
                return 0;
            }

            if (!_supplyStack.ContainsKey(stack.StackTypeId))
                return 0;

            return _supplyStack[stack.StackTypeId];
        }

        public int GetStackDemand(StackPriceComponent component, StackComponent stack)
        {
            if (stack.StackTypeId == null)
            {
                _sawmill.Error($"GetStackDemand: StackTypeId is null for {ToPrettyString(stack.Owner)}.");
                return 60;
            }

            if (!_demandStack.ContainsKey(stack.StackTypeId))
                return component.HalfPriceSurplus;

            return _demandStack[stack.StackTypeId];
        }

        public void AddReagentSupply(ReagentPrototype reagent, FixedPoint2 units)
        {
            if (_supplyReagent.ContainsKey(reagent.ID))
                _supplyReagent[reagent.ID] = FixedPoint2.Max(FixedPoint2.Zero, _supplyReagent[reagent.ID] + units);
            else
                _supplyReagent[reagent.ID] = FixedPoint2.Max(FixedPoint2.Zero, units);
        }

        public FixedPoint2 GetReagentSupply(ReagentPrototype reagent)
        {
            if (!_supplyReagent.ContainsKey(reagent.ID))
                return FixedPoint2.Zero;

            return _supplyReagent[reagent.ID];
        }

        public FixedPoint2 GetReagentDemand(ReagentPrototype reagent)
        {
            if (!_demandReagent.ContainsKey(reagent.ID))
                return reagent.HalfPriceSurplus;

            return _demandReagent[reagent.ID];
        }
    }
}
