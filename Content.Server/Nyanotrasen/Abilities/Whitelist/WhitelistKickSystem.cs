using Content.Server.Database;
using Content.Shared.GameTicking;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Whitelist;
public sealed class WhitelistKickSystem : EntitySystem
{
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IServerDbManager _db = default!;
        [Dependency] private readonly IServerNetManager _net = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRestart);
        }

        private async void OnRestart(RoundRestartCleanupEvent args)
        {
            if (!_cfg.GetCVar(CCVars.WhitelistEnabled))
                return;

            foreach (var session in _player.NetworkedSessions)
            {
                if (await _db.GetAdminDataForAsync(session.UserId) is not null)
                    continue;

                if (!await _db.GetWhitelistStatusAsync(session.UserId))
                {
                    _net.DisconnectChannel(session.ConnectedClient, Loc.GetString("whitelist-end-round-kick"));
                }
            }
        }
}
