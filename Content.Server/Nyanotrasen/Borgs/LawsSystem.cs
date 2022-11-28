using Content.Shared.Borgs;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;

namespace Content.Server.Borgs
{
    public sealed class LawsSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LawsComponent, StateLawsMessage>(OnStateLaws);
            SubscribeLocalEvent<LawsComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        private void OnStateLaws(EntityUid uid, LawsComponent component, StateLawsMessage args)
        {
            StateLaws(uid, component);
        }

        private void OnPlayerAttached(EntityUid uid, LawsComponent component, PlayerAttachedEvent args)
        {
            if (!_uiSystem.TryGetUi(uid, LawsUiKey.Key, out var ui))
                return;

            _uiSystem.TryOpen(uid, LawsUiKey.Key, args.Player);
        }

        public void StateLaws(EntityUid uid, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!component.CanState)
                return;

            if (component.StateTime != null && _timing.CurTime < component.StateTime)
                return;

            component.StateTime = _timing.CurTime + component.StateCD;

            foreach (var law in component.Laws)
            {
                _chat.TrySendInGameICMessage(uid, law, InGameICChatType.Speak, false);
            }
        }

        public void ClearLaws(EntityUid uid, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            component.Laws.Clear();
            Dirty(component);
        }

        public void AddLaw(EntityUid uid, string law, LawsComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            component.Laws.Add(law);
            Dirty(component);
        }
    }
}
