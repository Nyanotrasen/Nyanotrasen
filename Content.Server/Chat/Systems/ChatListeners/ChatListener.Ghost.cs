using Robust.Server.Player;
using Content.Shared.Radio;
using Content.Server.Ghost.Components;

namespace Content.Server.Chat.Systems
{
    /// <summary>
    /// This listener adds ghosts to all chat messages as a recipient.
    /// </summary>
    public sealed class GhostListenerSystem : ChatListenerSystem
    {
        private ISawmill _sawmill = default!;

        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            Before = new Type[] { typeof(SayListenerSystem), typeof(WhisperListenerSystem), typeof(EmoteListenerSystem), typeof(RadioListenerSystem) };

            InitializeListeners();

            _sawmill = Logger.GetSawmill("chat.ghost");
        }

        public override void OnGetRecipients(ref EntityChatGetRecipientsEvent args)
        {
            var ghosts = GetEntityQuery<GhostComponent>();

            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} playerEntity)
                    continue;

                if (!ghosts.HasComponent(playerEntity))
                    continue;

                var recipientData = new EntityChatData();

                // So ghosts get the radio message and never the obfuscated whispered version:
                if (args.Chat.TryGetData<RadioChannelPrototype[]>(ChatDataRadio.RadioChannels, out var radioChannels) &&
                    radioChannels.Length > 0)
                {
                    recipientData.SetData(ChatRecipientDataRadio.SharedRadioChannel, radioChannels[0]);
                    recipientData.SetData(ChatRecipientDataRadio.WillHearRadio, true);
                }

                args.Chat.Recipients.TryAdd(playerEntity, recipientData);
            }
        }
    }
}
