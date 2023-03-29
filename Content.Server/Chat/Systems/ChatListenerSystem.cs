using Content.Server.Chat.Managers;

namespace Content.Server.Chat.Systems
{
    public abstract class ChatListenerSystem : EntitySystem
    {
        [Dependency] protected readonly IChatManager _chatManager = default!;

        [Flags]
        public enum EnabledListener : int
        {
            None = 0,
            ParseChat = 1 << 0,
            AttemptChat = 1 << 1,
            GetRecipients = 1 << 2,
            TransformChat = 1 << 3,
            AfterTransform = 1 << 4,
            BeforeChat = 1 << 5,
            RecipientTransformChat = 1 << 6,
            Chat = 1 << 7,
        }

        /// <summary>
        /// This flag determines which event subscriptions are made, to avoid subscribing to irrelevant events.
        /// </summary>
        protected EnabledListener EnabledListeners;

        protected Type[]? ListenBefore;
        protected Type[]? ListenAfter;

        public override void Initialize()
        {
            base.Initialize();

            if (EnabledListeners.HasFlag(EnabledListener.ParseChat))
                SubscribeLocalEvent<EntityChatParseEvent>(OnParseChat, before: ListenBefore, after: ListenAfter);

            if (EnabledListeners.HasFlag(EnabledListener.AttemptChat))
                SubscribeLocalEvent<EntityChatAttemptEvent>(OnChatAttempt, before: ListenBefore, after: ListenAfter);

            if (EnabledListeners.HasFlag(EnabledListener.GetRecipients))
                SubscribeLocalEvent<EntityChatGetRecipientsEvent>(OnGetRecipients, before: ListenBefore, after: ListenAfter);

            if (EnabledListeners.HasFlag(EnabledListener.TransformChat))
                SubscribeLocalEvent<EntityChatTransformEvent>(OnTransformChat, before: ListenBefore, after: ListenAfter);

            if (EnabledListeners.HasFlag(EnabledListener.AfterTransform))
                SubscribeLocalEvent<EntityChatAfterTransformEvent>(AfterTransform, before: ListenBefore, after: ListenAfter);

            if (EnabledListeners.HasFlag(EnabledListener.BeforeChat))
                SubscribeLocalEvent<BeforeEntityChatEvent>(BeforeChat, before: ListenBefore, after: ListenAfter);

            if (EnabledListeners.HasFlag(EnabledListener.RecipientTransformChat))
                SubscribeLocalEvent<GotEntityChatTransformEvent>(OnRecipientTransformChat, before: ListenBefore, after: ListenAfter);

            if (EnabledListeners.HasFlag(EnabledListener.Chat))
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
