using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Exists just to listen to a single event. What a life.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed class SlowsOnContactComponent : Component
{
}
