using Content.Shared.Redial;
using Robust.Shared.Random;
using Robust.Client;
namespace Content.Client.Redial;

// We can't raise network event outside of an entity system I don't think...
public sealed class RedialSystem : EntitySystem
{
    public void TryRedialToRandom()
    {
        RaiseNetworkEvent(new RequestRandomRedialServer());
    }
}
