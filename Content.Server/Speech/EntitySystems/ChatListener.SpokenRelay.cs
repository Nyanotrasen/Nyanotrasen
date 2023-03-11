using Content.Server.Chat.Systems;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems
{
    /// <summary>
    /// This listener relays all spoken messages to entities listening for EntitySpoke and Listen events.
    /// It replaces ListeningSystem and is a stand-in for the old systems.
    /// </summary>
    public sealed class SpokenRelayListenerSystem : ChatListenerSystem
    {
        private ISawmill _sawmill = default!;

        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            InitializeListeners();

            _sawmill = Logger.GetSawmill("chat.spoken");
        }

        public override void AfterTransform(ref EntityChatAfterTransformEvent args)
        {
            if (!args.Chat.GetData<bool>(ChatDataSay.IsSpoken))
                return;

            // Inform old systems...
            var spokeEvent = new EntitySpokeEvent(args.Chat);
            RaiseLocalEvent(args.Chat.Source, spokeEvent, true);

            // NOTE: most code below taken from ListeningSystem, adjust as needed.

            // TODO whispering / audio volume? Microphone sensitivity?
            // for now, whispering just arbitrarily reduces the listener's max range.

            // Inform ActiveListeners...
            var xformQuery = GetEntityQuery<TransformComponent>();
            var sourceXform = xformQuery.GetComponent(args.Chat.Source);
            var sourcePos = _transformSystem.GetWorldPosition(sourceXform, xformQuery);

            var listenAttempt = new ListenAttemptEvent(args.Chat.Source);
            var listenEvent = new ListenEvent(args.Chat);

            foreach (var (listener, xform) in EntityQuery<ActiveListenerComponent, TransformComponent>())
            {
                if (xform.MapID != sourceXform.MapID)
                    return;

                // range checks
                // TODO proper speech occlusion
                var distance = (sourcePos - _transformSystem.GetWorldPosition(xform, xformQuery)).Length;
                if (distance > listener.Range)
                    continue;

                RaiseLocalEvent(listener.Owner, listenAttempt);
                if (listenAttempt.Cancelled)
                {
                    listenAttempt.Uncancel();
                    continue;
                }

                RaiseLocalEvent(listener.Owner, listenEvent);
            }
        }
    }
}
