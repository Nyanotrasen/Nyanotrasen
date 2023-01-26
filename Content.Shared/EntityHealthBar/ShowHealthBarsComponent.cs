using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityHealthBar
{
    [RegisterComponent]
    public sealed class ShowHealthBarsComponent : Component
    {
        /// <summary>
        /// Whether we should occlude entities we couldn't otherwise examine.
        /// </summary>
        [DataField("checkLOS")]
        public bool CheckLOS = false;

        /// <summary>
        /// If null, displays all health bars.
        /// If not null, displays health bars of only that damage container.
        /// </summary>

        [DataField("damageContainer", customTypeSerializer: typeof(PrototypeIdSerializer<DamageContainerPrototype>))]
        public string? DamageContainer;
    }
}
