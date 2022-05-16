using Content.Shared.Interaction;
using Content.Server.Chat;
using Content.Server.Weapon.Melee.Components;

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

        private void OnAfterInteractUsing(EntityUid uid, SophicScribeComponent component, AfterInteractUsingEvent args)
        {
            if (args.Used == null)
                return;

            _chat.TrySendInGameICMessage(uid, Loc.GetString("sophic-entity-name", ("item", args.Used)), InGameICChatType.Speak, true);

            if (TryComp<MeleeWeaponComponent>(args.Used, out var melee))
            {
                _chat.TrySendInGameICMessage(uid, AssembleReport(melee), InGameICChatType.Speak, true);
            }
        }
    }
}
