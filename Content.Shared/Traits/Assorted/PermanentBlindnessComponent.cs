using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for making something blind forever.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class PermanentBlindnessComponent : Component
{
    /// <summary>
    /// Items to give (i.e. cane).
    /// </summary>
    [DataField("blindGear", required: false, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BlindGear = string.Empty;
}

