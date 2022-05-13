using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Salvage
{
    [Prototype("salvageMap")]
    public sealed class SalvageMapPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        /// <summary>
        /// Relative directory path to the given map, i.e. `Maps/Salvage/test.yml`
        /// </summary>
        [ViewVariables]
        [DataField("mapPath", required: true)]
        public ResourcePath MapPath { get; } = default!;

        /// <summary>
        /// Size *from 0,0* in units of the map (used to determine if it fits)
        /// </summary>
        [ViewVariables]
        [DataField("size", required: true)]
        public float Size { get; } = 1.0f; // TODO: Find a way to figure out the size automatically

        /// <summary>
        /// Name for admin use
        /// </summary>
        [ViewVariables]
        [DataField("name")]
        public string Name { get; } = "";
    }
}
