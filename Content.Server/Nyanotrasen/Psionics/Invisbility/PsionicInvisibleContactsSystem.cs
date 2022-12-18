using Content.Shared.Abilities.Psionics;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Psionics
{
    public sealed class PsionicInvisibleContactsSystem : EntitySystem
    {
        [Dependency] private readonly SharedStealthSystem _stealth = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
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

                if (!contact.Value.IsTouching)
                    continue;

                // TODO: I am unsure this is still neccesary with the introduction of IsTouching above.
                // yes the tick checks are kind of shitty and tickrate dependent
                // what's even shittier is that you can apparently still be colliding with an entity for multiple ticks
                // after EndCollideEvent is raised
                if (TryComp<PsionicInvisibleContactsComponent>(contact.Key.Body.Owner, out var psiontacts)
                    && psiontacts.Whitelist.IsValid(otherUid)
                    && psiontacts.LastFailedTick !<= (_gameTiming.CurTick - 5))
                {
                    component.LastFailedTick = _gameTiming.CurTick;
                    return;
                }
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
