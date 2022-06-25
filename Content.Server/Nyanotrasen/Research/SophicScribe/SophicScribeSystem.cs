using Content.Shared.Interaction;
using Content.Server.Chat.Systems;

namespace Content.Server.Research.SophicScribe
{
    public sealed partial class SophicScribeSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SophicScribeComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var scribe in EntityQuery<SophicScribeComponent>())
            {
                if (scribe.SpeechQueue.Count == 0)
                {
                    scribe.Accumulator = 4.5f;
                    continue;
                }

                scribe.Accumulator += frameTime;
                if (scribe.Accumulator < scribe.SpeechDelay.TotalSeconds)
                {
                    continue;
                }

                _chat.TrySendInGameICMessage(scribe.Owner, scribe.SpeechQueue.Dequeue(), InGameICChatType.Speak, true);
                scribe.Accumulator = 0;
            }
        }

        private void OnAfterInteractUsing(EntityUid uid, SophicScribeComponent component, AfterInteractUsingEvent args)
        {
            if (args.Used == null || !args.CanReach)
                return;

            if (component.SpeechQueue.Count != 0)
                return;

            AssembleReport(args.Used, uid, component);
        }
    }
}
