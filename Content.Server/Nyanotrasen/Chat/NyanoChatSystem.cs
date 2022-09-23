using System.Linq;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Nyanotrasen.Chat
{
    /// <summary>
    /// Extensions for nyano's chat stuff
    /// </summary>

    public sealed class NyanoChatSystem : EntitySystem
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
        private IEnumerable<INetChannel> GetPsionicChatClients()
        {
            return Filter.Empty()
                .AddWhereAttachedEntity(entity => HasComp<PsionicComponent>(entity) && !HasComp<PsionicsDisabledComponent>(entity) && !HasComp<PsionicInsulationComponent>(entity))
                .Recipients
                .Union(_adminManager.ActiveAdmins)
                .Select(p => p.ConnectedClient);
        }

        public void SendTelepathicChat(EntityUid source, string message, bool hideChat)
        {
            if (!HasComp<PsionicComponent>(source) || HasComp<PsionicsDisabledComponent>(source) || HasComp<PsionicInsulationComponent>(source))
                return;

            var clients = GetPsionicChatClients();
            string messageWrap;

            messageWrap = Loc.GetString("chat-manager-send-telepathic-chat-wrap-message",
                ("telepathicChannelName", Loc.GetString("chat-manager-telepathic-channel-name")));

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telepathic chat from {source:Player}: {message}");

            _chatManager.ChatMessageToMany(ChatChannel.Telepathic, message, messageWrap, source, hideChat, clients.ToList(), Color.PaleVioletRed);

            if (_random.Prob(0.1f))
                _glimmerSystem.AddToGlimmer(1);
        }
    }
}
