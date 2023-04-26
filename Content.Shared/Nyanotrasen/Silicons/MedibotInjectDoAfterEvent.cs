using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;

namespace Content.Shared.Silicons
{
    [Serializable, NetSerializable]
    public sealed class MedibotInjectDoAfterEvent : DoAfterEvent
    {
        [DataField("solution", required: true)]
        public Solution Solution = default!;

        [DataField("amount", required: true)]
        public FixedPoint2 Amount;

        [DataField("drug", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
        public string Drug = default!;

        private MedibotInjectDoAfterEvent()
        {
        }

        public MedibotInjectDoAfterEvent(Solution solution, string drug, FixedPoint2 amount)
        {
            Solution = solution;
            Drug = drug;
            Amount = amount;
        }

        public override DoAfterEvent Clone() => this;
    }
}
