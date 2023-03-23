using Content.Server.Chat.Systems;
using Content.Shared.Mobs;
using Content.Shared.Tag;
using Content.Shared.IdentityManagement;

namespace Content.Server.Borgs
{
    public sealed class CyborgDeath : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CyborgDeathComponent, MobStateChangedEvent>(OnChangeState);
        }
        
        private void OnChangeState(EntityUid uid, CyborgDeathComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead){
                string message = Loc.GetString("death-gasp-borg", ("ent", Identity.Entity(uid, EntityManager)));

                _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Emote, false, force:true);

                _tagSystem.RemoveTag(uid, "DoorBumpOpener"); //Stop dead borg from being movable AA cards.
            }
            else if(args.NewMobState == MobState.Alive && args.OldMobState == MobState.Dead)
            {
                _tagSystem.AddTag(uid, "DoorBumpOpener");
            }
            else
                return;
        }
    }
}