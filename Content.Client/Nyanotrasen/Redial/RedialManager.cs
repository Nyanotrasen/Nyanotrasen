using Content.Shared.Redial;
using Robust.Shared.Network;
using Robust.Client;

namespace Content.Client.Redial
{
    public sealed class RedialManager
    {
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IClientNetManager _net = default!;

        public void Initialize()
        {
            _net.RegisterNetMessage<MsgRedialServer>(RxRedialServer);
        }

        private void RxRedialServer(MsgRedialServer msg)
        {
            _gameController.Redial(msg.Server, "Connecting to another server...");
        }
    }
}
