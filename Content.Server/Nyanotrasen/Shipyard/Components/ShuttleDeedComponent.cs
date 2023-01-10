using Content.Shared.Shipyard;

namespace Content.Server.Shipyard.Components;

/// <summary>
/// Stores the purchased shuttle ID onto a component to store somewhere for later retrieval ie selling.
/// </summary>
[RegisterComponent, Access(typeof(SharedShipyardSystem))]
public sealed class ShuttleDeedComponent : Component
{
    [DataField("shuttleuid")]
    public EntityUid? ShuttleUid;
}
