using Content.Shared.Kitchen.Components;

namespace Content.Server.Kitchen.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDeepFriedComponent))]
    public sealed class DeepFriedComponent : SharedDeepFriedComponent
    {
        /// <summary>
        /// What is the item's base price multiplied by?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("priceCoefficient")]
        public float PriceCoefficient { get; set; } = 1.0f;
    }
}
