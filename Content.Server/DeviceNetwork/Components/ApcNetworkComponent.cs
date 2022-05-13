using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.Nodes;

namespace Content.Server.DeviceNetwork.Components
{
    [RegisterComponent]
    [Friend(typeof(ApcNetworkSystem))]
    [ComponentProtoName("ApcNetworkConnection")]
    public sealed class ApcNetworkComponent : Component
    {
        /// <summary>
        /// The node Group the ApcNetworkConnection is connected to
        /// </summary>
        [ViewVariables] public Node? ConnectedNode;
    }
}
