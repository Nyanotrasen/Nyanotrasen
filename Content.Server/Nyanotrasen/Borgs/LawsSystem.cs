using Content.Shared.Borgs;

namespace Content.Server.Borgs
{
    public sealed class LawsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LawsComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, LawsComponent component, ComponentInit args)
        {
        }
    }
}
