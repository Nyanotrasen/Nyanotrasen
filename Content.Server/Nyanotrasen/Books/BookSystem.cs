using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.Books;

namespace Content.Server.Books
{
    public sealed class SharedBookSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HyperlinkBookComponent, ActivateInWorldEvent>(OnActivate);
        }

        private void OnActivate(EntityUid uid, HyperlinkBookComponent component, ActivateInWorldEvent args)
        {
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            var session = actor.PlayerSession;


            var ev = new OpenURLEvent(component.URL);
            RaiseNetworkEvent(ev, session.ConnectedClient);
        }
    }
}
