using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Abilities.Psionics;
using Content.Server.Psionics;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Interaction;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems
{
    /// <summary>
    /// Handles psionic artifacts
    /// </summary>
    public sealed class PsionicArtifactSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly PsionicAbilitiesSystem _psionicAbilitiesSystem = default!;
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicArtifactComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<PsionicArtifactComponent, ArtifactActivatedEvent>(OnActivate);
        }

        /// <summary>
        /// Initialize charges
        /// </summary>
        private void OnMapInit(EntityUid uid, PsionicArtifactComponent component, MapInitEvent args)
        {
            component.Charges += _random.Next(1, 3);
        }

        /// <summary>
        /// When activated, blasts everyone in LOS within n tiles
        /// with a psionic-granting beam
        /// </summary>
        private void OnActivate(EntityUid uid, PsionicArtifactComponent component, ArtifactActivatedEvent args)
        {
            var xform = Transform(uid);
            var potentialQuery = GetEntityQuery<PotentialPsionicComponent>();

            foreach (var entity in _lookup.GetEntitiesInRange(xform.Coordinates, component.Range))
            {
                if (component.Charges == 0) return;

                if (!potentialQuery.TryGetComponent(entity, out var potential)) continue;
                if (HasComp<PsionicComponent>(entity) || HasComp<PsionicInsulationComponent>(entity)) continue;

                if (!_interactionSystem.InRangeUnobstructed(uid, entity, component.Range))
                    continue;

                _psionicAbilitiesSystem.AddPsionics(entity);
                _glimmerSystem.Glimmer += _random.Next(1, 3);
                component.Charges--;
            }
        }
    }
}

