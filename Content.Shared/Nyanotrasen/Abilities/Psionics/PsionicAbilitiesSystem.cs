using Robust.Shared.Random;

namespace Content.Shared.Abilities.Psionics
{
    public sealed class PsionicAbilitiesSystem : EntitySystem
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        public readonly IReadOnlyList<string> PsionicPowerPool = new[]
        {
            "PacificationPower",
            "MetapsionicPower",
        };
        public void AddPsionics(EntityUid uid)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            AddComp<PsionicComponent>(uid);

            // uh oh, stinky!
            var newComponent = (Component) _componentFactory.GetComponent(_random.Pick(PsionicPowerPool));
            newComponent.Owner = uid;

            EntityManager.AddComponent(uid, newComponent);
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
    }
}