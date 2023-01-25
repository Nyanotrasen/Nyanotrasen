using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Redial;

/// <summary>
/// Sent server -> client to ask the client to redial to a certain server.
/// </summary>
public sealed class MsgRedialServer : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public string Server = "";

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Server = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Server);
    }
}
