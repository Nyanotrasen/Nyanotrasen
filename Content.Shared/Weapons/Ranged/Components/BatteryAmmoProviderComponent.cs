namespace Content.Shared.Weapons.Ranged.Components;

public abstract class BatteryAmmoProviderComponent : AmmoProviderComponent
{
    /// <summary>
    /// How much battery it costs to fire once.
    /// </summary>
    [DataField("fireCost")]
    public float FireCost = 100;

    /// <summary>
    /// Whether to show examine text.
    /// </summary>
    [DataField("examinable")]
    public bool Examinable = true;

    /// <summary>
    /// Whether to consume ammo.
    /// </summary>
    [DataField("infinite")]
    public bool Infinite = false;

    // Batteries aren't predicted which means we need to track the battery and manually count it ourselves woo!

    [ViewVariables]
    public int Shots;

    [ViewVariables]
    public int Capacity;
}
