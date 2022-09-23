using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Abilities.Psionics;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.MobState;
using Content.Shared.ActionBlocker;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Nyanotrasen.Chat
{
    /// <summary>
    /// Extensions for nyano's chat stuff
    /// </summary>

    public sealed class NyanoChatSystem : EntitySystem
    {
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly ListeningSystem _listener = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
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
        }
    }
}
