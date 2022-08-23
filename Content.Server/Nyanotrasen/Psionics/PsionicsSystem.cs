using Content.Shared.Abilities.Psionics;
using Robust.Shared.Random;

namespace Content.Server.Psionics
{
    public sealed class PsionicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PotentialPsionicComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, PotentialPsionicComponent component, ComponentInit args)
        {
            if (_random.Prob(component.Chance))
                _psionicAbilitiesSystem.AddPsionics(uid);
        }
    }
}
