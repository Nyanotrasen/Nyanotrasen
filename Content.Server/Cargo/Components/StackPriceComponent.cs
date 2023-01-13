namespace Content.Server.Cargo.Components;

/// <summary>
/// This is used for pricing stacks of items.
/// </summary>
[RegisterComponent]
public sealed class StackPriceComponent : Component
{
    /// <summary>
    /// The price of the object this component is on, per unit.
    /// </summary>
    [DataField("price", required: true)]
    public double Price;

    /// <summary>
    /// How many surplus units of this stack need to be on the market before
    /// the price reaches half of its default price?
    /// </summary>
    [DataField("halfPriceSurplus")]
    public int HalfPriceSurplus = 60;
}
