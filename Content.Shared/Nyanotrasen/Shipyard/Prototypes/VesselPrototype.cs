using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Shipyard.Prototypes
{
    [NetSerializable, Serializable, Prototype("vessel")]
    public sealed class VesselPrototype : IPrototype
    {
        [DataField("name")] private string _name = string.Empty;

        [DataField("description")] private string _description = string.Empty;

        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        ///     Vessel name.
        /// </summary>
        [ViewVariables]
        public string Name
        {
            get
            {
                if (_name.Trim().Length != 0)
                    return _name;

                if (IoCManager.Resolve<IPrototypeManager>().TryIndex(Vessel, out EntityPrototype? prototype))
                {
                    _name = prototype.Name;
                }

                return _name;
            }
        }

        /// <summary>
        ///     Short description of the product.
        /// </summary>
        [ViewVariables]
        public string Description
        {
            get
            {
                if (_description.Trim().Length != 0)
                    return _description;

                if (IoCManager.Resolve<IPrototypeManager>().TryIndex(Vessel, out EntityPrototype? prototype))
                {
                    _description = prototype.Description;
                }

                return _description;
            }
        }

        /// <summary>
        ///     The prototype name of the vessel.
        /// </summary>
        [DataField("vessel", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Vessel { get; } = string.Empty;

        /// <summary>
        ///     The price of the vessel
        /// </summary>
        [DataField("price", required: true)]
        public int Price { get; }

        /// <summary>
        ///     The prototype category of the product. (e.g. Small, Medium, Large, Emergency, Special etc.)
        /// </summary>
        [DataField("category")]
        public string Category { get; } = string.Empty;

        /// <summary>
        ///     The prototype group of the product. (e.g. Civilian, Syndicate, Contraband etc.)
        /// </summary>
        [DataField("group")]
        public string Group { get; } = string.Empty;

        /// <summary>
        ///     Relative directory path to the given shuttle, i.e. `/Maps/saltern.yml`
        /// </summary>
        [DataField("shuttlePath", required: true)]
        public ResourcePath ShuttlePath { get; } = default!;
    }
}
