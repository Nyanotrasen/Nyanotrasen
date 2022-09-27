using Content.Shared.Abilities.Psionics;
using Content.Client.Chat.Managers;

namespace Content.Client.Nyanotrasen.Chat
{
    public sealed class PsionicChatUpdateSystem : EntitySystem
    {
        [Dependency] private readonly IChatManager _chatManager = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<PsionicsChangedEvent>(OnPsionicsChanged);
        }

        private void OnPsionicsChanged(PsionicsChangedEvent args)
        {
            _chatManager.UpdateChannelPermissions();
        }
    }
}
