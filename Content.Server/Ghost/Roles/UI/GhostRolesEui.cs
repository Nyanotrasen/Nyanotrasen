using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;
using Content.Server.Redial;

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class GhostRolesEui : BaseEui
    {
        public override GhostRolesEuiState GetNewState()
        {
            // trying to do this the way the obsolete warning suggests results in an NRE :^)
            var roles = EntitySystem.Get<GhostRoleSystem>().GetGhostRolesInfo();
            var enabled = IoCManager.Resolve<RedialManager>().RedialAvailable();
            return new GhostRolesEuiState(roles, enabled);
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case GhostRoleTakeoverRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().Takeover(Player, req.Identifier);
                    break;
                case GhostRoleFollowRequestMessage req:
                    EntitySystem.Get<GhostRoleSystem>().Follow(Player, req.Identifier);
                    break;
                case GhostRoleWindowCloseMessage _:
                    Closed();
                    break;
            }
        }

        public override void Closed()
        {
            base.Closed();

            EntitySystem.Get<GhostRoleSystem>().CloseEui(Player);
        }
    }
}
