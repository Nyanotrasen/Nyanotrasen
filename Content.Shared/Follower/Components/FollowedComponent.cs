using Robust.Shared.GameStates;

namespace Content.Shared.Follower.Components;

// TODO properly network this and followercomp.
/// <summary>
///     Attached to entities that are currently being followed by a ghost.
/// </summary>
[RegisterComponent, Friend(typeof(FollowerSystem))]
[NetworkedComponent]
public sealed class FollowedComponent : Component
{
    public HashSet<EntityUid> Following = new();
}
