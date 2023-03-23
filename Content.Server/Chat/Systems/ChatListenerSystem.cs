using Content.Server.Chat.Managers;

namespace Content.Server.Chat.Systems
{
    public abstract class ChatListenerSystem : EntitySystem
    {
        [Dependency] protected readonly IChatManager _chatManager = default!;

        protected Type[]? ListenBefore;
        protected Type[]? ListenAfter;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntityChatParseEvent>(OnParseChat, before: ListenBefore, after: ListenAfter);
            SubscribeLocalEvent<EntityChatAttemptEvent>(OnChatAttempt, before: ListenBefore, after: ListenAfter);
            SubscribeLocalEvent<EntityChatGetRecipientsEvent>(OnGetRecipients, before: ListenBefore, after: ListenAfter);
            SubscribeLocalEvent<EntityChatTransformEvent>(OnTransformChat, before: ListenBefore, after: ListenAfter);
            SubscribeLocalEvent<EntityChatAfterTransformEvent>(AfterTransform, before: ListenBefore, after: ListenAfter);
            SubscribeLocalEvent<BeforeEntityChatEvent>(BeforeChat, before: ListenBefore, after: ListenAfter);
            SubscribeLocalEvent<GotEntityChatTransformEvent>(OnRecipientTransformChat, before: ListenBefore, after: ListenAfter);
            SubscribeLocalEvent<GotEntityChatEvent>(OnChat, before: ListenBefore, after: ListenAfter);
        }

        public virtual void OnParseChat(ref EntityChatParseEvent args) { }
        public virtual void OnChatAttempt(ref EntityChatAttemptEvent args) { }
        public virtual void OnGetRecipients(ref EntityChatGetRecipientsEvent args) { }
        public virtual void OnTransformChat(ref EntityChatTransformEvent args) { }
        public virtual void AfterTransform(ref EntityChatAfterTransformEvent args) { }
        public virtual void BeforeChat(ref BeforeEntityChatEvent args) { }
        public virtual void OnRecipientTransformChat(ref GotEntityChatTransformEvent args) { }
        public virtual void OnChat(ref GotEntityChatEvent args) { }
    }
}
