using Content.Server.Radio.Components;
using Content.Shared.Chat;
using Robust.Server.GameObjects;
using Robust.Shared.Network;

namespace Content.Server.Ghost.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IRadio))]
    public sealed class GhostRadioComponent : Component, IRadio
    {
        [Dependency] private readonly IServerNetManager _netManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        [DataField("channels")]
        private List<int> _channels = new(){1459};

        public IReadOnlyList<int> Channels => _channels;

        public void Receive(string message, int channel, EntityUid speaker)
        {
            if (!_entMan.TryGetComponent(Owner, out ActorComponent? actor))
                return;

            var playerChannel = actor.PlayerSession.ConnectedClient;

            var msg = new MsgChatMessage();

            msg.Channel = ChatChannel.Radio;
            msg.Message = message;
            //Square brackets are added here to avoid issues with escaping
            msg.MessageWrap = Loc.GetString("chat-radio-message-wrap", ("channel", $"\\[{channel}\\]"), ("name", _entMan.GetComponent<MetaDataComponent>(speaker).EntityName));
            _netManager.ServerSendMessage(msg, playerChannel);
        }

        public void Broadcast(string message, EntityUid speaker) { }
    }
}
