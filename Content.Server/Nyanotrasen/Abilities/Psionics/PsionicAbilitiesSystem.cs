using Content.Shared.Abilities.Psionics;
using Content.Server.Administration;
using Robust.Shared.Random;
using Robust.Server.GameObjects;

namespace Content.Server.Abilities.Psionics
{
    public sealed class PsionicAbilitiesSystem : EntitySystem
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly QuickDialogSystem _quickDialogSystem = default!;
        public readonly IReadOnlyList<string> PsionicPowerPool = new[]
        {
            "PacificationPower",
            "MetapsionicPower",
            "DispelPower",
            "MassSleepPower",
        };

        public void AddPsionics(EntityUid uid)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            if (!HasComp<GuaranteedPsionicComponent>(uid) && TryComp<ActorComponent>(uid, out var actor))
            {
                _quickDialogSystem.OpenDialog(actor.PlayerSession, "Psionic!", "You've rolled a psionic power. The forensic mantis may hunt you, so you'll want to keep it a secret. Do you still want to be psychic?", (string response) => AddRandomPsionicPower(uid), null);
                return;
            }

            AddRandomPsionicPower(uid);
        }

        public void AddPsionics(EntityUid uid, string powerComp)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            AddComp<PsionicComponent>(uid);

            var newComponent = (Component) _componentFactory.GetComponent(powerComp);
            newComponent.Owner = uid;

            EntityManager.AddComponent(uid, newComponent);
        }

        private void AddRandomPsionicPower(EntityUid uid)
        {
            AddComp<PsionicComponent>(uid);

            // uh oh, stinky!
            var newComponent = (Component) _componentFactory.GetComponent(_random.Pick(PsionicPowerPool));
            newComponent.Owner = uid;

            EntityManager.AddComponent(uid, newComponent);
        }
    }
}
