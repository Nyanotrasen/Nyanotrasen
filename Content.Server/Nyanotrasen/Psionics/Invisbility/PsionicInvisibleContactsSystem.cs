using Content.Shared.Abilities.Psionics;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Psionics
{
    public sealed class PsionicInvisibleContactsSystem : EntitySystem
    {
        [Dependency] private readonly SharedStealthSystem _stealth = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicInvisibleContactsComponent, StartCollideEvent>(OnEntityEnter);
            SubscribeLocalEvent<PsionicInvisibleContactsComponent, EndCollideEvent>(OnEntityExit);

            UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        }

        private void OnEntityEnter(EntityUid uid, PsionicInvisibleContactsComponent component, ref StartCollideEvent args)
        {
            var otherUid = args.OtherFixture.Body.Owner;

            if (!component.Whitelist.IsValid(otherUid))
                return;

            if (HasComp<PsionicInvisibilityUsedComponent>(otherUid))
                return;

            EnsureComp<PsionicallyInvisibleComponent>(otherUid);
            var stealth = EnsureComp<StealthComponent>(otherUid);
            _stealth.SetVisibility(otherUid, 0.66f, stealth);
        }

        private void OnEntityExit(EntityUid uid, PsionicInvisibleContactsComponent component, ref EndCollideEvent args)
        {
            var otherUid = args.OtherFixture.Body.Owner;

            foreach (var contact in args.OtherFixture.Contacts)
            {
                if (contact.Key.Body.Owner == uid)
                    continue;

                if (TryComp<PsionicInvisibleContactsComponent>(contact.Key.Body.Owner, out var psiontacts)
                    && psiontacts.Whitelist.IsValid(otherUid))
                    return;
            }

            if (!component.Whitelist.IsValid(otherUid))
                return;

            if (HasComp<PsionicInvisibilityUsedComponent>(otherUid))
                return;

            RemComp<PsionicallyInvisibleComponent>(otherUid);
            RemComp<StealthComponent>(otherUid);
        }
    }

}
