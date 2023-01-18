namespace Content.Server.Cargo.Components;

/// <summary>
/// This component indicates an entity with a price that is subject to the laws
/// of supply and demand.
/// </summary>
[RegisterComponent]
public sealed class DynamicPriceComponent : Component
{
    /// <summary>
    /// In a neutral environment, what does this entity cost?
    /// </summary>
    [DataField("price", required: true)]
    public double Price;

    /// <summary>
    /// How many surplus units of this entity need to be on the market before
    /// the price reaches half of its default price?
    /// </summary>
    [DataField("halfPriceSurplus")]
    public int HalfPriceSurplus = 5;

    /// <summary>
    /// What arbitrary identifier represents this entity in the marketplace?
    /// </summary
    /// <remarks>
    /// This can be used to group similar items so the sale of one has
    /// influence on the price of another. If one is not specified, the system
    /// defaults to the entity prototype ID.
    /// </remarks>
    [DataField("commodity")]
    public string? CommodityId;
}
