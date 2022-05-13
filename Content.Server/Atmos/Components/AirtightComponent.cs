using Content.Shared.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class AirtightComponent : Component
    {
        public (GridId Grid, Vector2i Tile) LastPosition { get; set; }

        [DataField("airBlockedDirection", customTypeSerializer: typeof(FlagSerializer<AtmosDirectionFlags>))]
        [ViewVariables]
        public int InitialAirBlockedDirection { get; set; } = (int) AtmosDirection.All;

        [ViewVariables]
        public int CurrentAirBlockedDirection;

        [DataField("airBlocked")]
        public bool AirBlocked { get; set; } = true;

        [DataField("fixVacuum")]
        public bool FixVacuum { get; set; } = true;

        [ViewVariables]
        [DataField("rotateAirBlocked")]
        public bool RotateAirBlocked { get; set; } = true;

        [ViewVariables]
        [DataField("fixAirBlockedDirectionInitialize")]
        public bool FixAirBlockedDirectionInitialize { get; set; } = true;

        [ViewVariables]
        [DataField("noAirWhenFullyAirBlocked")]
        public bool NoAirWhenFullyAirBlocked { get; set; } = true;

        public AtmosDirection AirBlockedDirection => (AtmosDirection)CurrentAirBlockedDirection;
    }
}
