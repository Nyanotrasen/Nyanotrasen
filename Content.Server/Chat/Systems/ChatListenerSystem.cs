using Content.Server.Chat.Managers;

namespace Content.Server.Chat.Systems
{
    public abstract class ChatListenerSystem : EntitySystem
    {
        [Dependency] protected readonly IChatManager _chatManager = default!;

        protected Type[]? Before;
        protected Type[]? After;

        protected void InitializeListeners()
        {
            SubscribeLocalEvent<EntityChatParseEvent>(OnParseChat, before: Before, after: After);
            SubscribeLocalEvent<EntityChatAttemptEvent>(OnChatAttempt, before: Before, after: After);
            SubscribeLocalEvent<EntityChatGetRecipientsEvent>(OnGetRecipients, before: Before, after: After);
            SubscribeLocalEvent<EntityChatTransformEvent>(OnTransformChat, before: Before, after: After);
            SubscribeLocalEvent<BeforeEntityChatEvent>(BeforeChat, before: Before, after: After);
            SubscribeLocalEvent<GotEntityChatEvent>(OnChat, before: Before, after: After);
        }

        public virtual void OnParseChat(ref EntityChatParseEvent args) { }
        public virtual void OnChatAttempt(ref EntityChatAttemptEvent args) { }
        public virtual void OnGetRecipients(ref EntityChatGetRecipientsEvent args) { }
        public virtual void OnTransformChat(ref EntityChatTransformEvent args) { }
        public virtual void BeforeChat(ref BeforeEntityChatEvent args) { }
        public virtual void OnChat(ref GotEntityChatEvent args) { }
    }
}
