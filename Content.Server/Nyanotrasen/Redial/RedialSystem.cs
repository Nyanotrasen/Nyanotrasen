using Content.Shared.Redial;

namespace Content.Server.Redial;

public sealed class RedialSystem : EntitySystem
{
    [Dependency] private readonly RedialManager _redial = default!;
    public override void Initialize()
    {
        SubscribeNetworkEvent<RequestRandomRedialServer>(OnRequestRedial);
    }

    private void OnRequestRedial(RequestRandomRedialServer msg, EntitySessionEventArgs args)
    {
        _redial.SendRedialMessage(args.SenderSession);
    }
}
