using Content.Shared.Interaction;
using Content.Shared.Abilities.Psionics;
using Content.Server.Chat.Systems;

namespace Content.Server.Research.SophicScribe
{
    public sealed partial class SophicScribeSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SophicScribeComponent, InteractHandEvent>(OnInteractHand);
        }
        private void OnInteractHand(EntityUid uid, SophicScribeComponent component, InteractHandEvent args)
        {
            _chat.TrySendInGameICMessage(uid, Loc.GetString("glimmer-report", ("level", _sharedGlimmerSystem.Glimmer)), InGameICChatType.Speak, true);
        }
    }
}
