using Content.Shared.Redial;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Client;
namespace Content.Client.Redial;

public sealed class RedialSystem : EntitySystem
{
    [Dependency] private readonly IGameController _gameController = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    /// <summary>
    /// True if we have valid servers, false otherwise.
    public void TryRedialToRandom()
    {
        RaiseNetworkEvent(new RequestRedialServersMessage());
    }
}
