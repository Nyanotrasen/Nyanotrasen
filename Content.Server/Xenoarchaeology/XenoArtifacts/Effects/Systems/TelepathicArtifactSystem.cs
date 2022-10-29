using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Popups;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Psionics.Glimmer;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class TelepathicArtifactSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelepathicArtifactComponent, ArtifactActivatedEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, TelepathicArtifactComponent component, ArtifactActivatedEvent args)
    {
        // try to find victims nearby
        var victims = _lookup.GetEntitiesInRange(uid, component.Range);
        foreach (var victimUid in victims)
        {
            if (!EntityManager.HasComponent<ActorComponent>(victimUid))
                continue;

            if (HasComp<PsionicInsulationComponent>(victimUid))
                continue;

            // roll if msg should be usual or drastic
            var isDrastic = _random.NextFloat() <= component.DrasticMessageProb;
            var msgArr = isDrastic ? component.DrasticMessages : component.Messages;

            // pick a random message
            var msgId = _random.Pick(msgArr);
            var msg = Loc.GetString(msgId);

            // show it as a popup, but only for the victim
            _popupSystem.PopupEntity(msg, victimUid, Filter.Entities(victimUid));
            if (_random.Prob(0.05f))
                _sharedGlimmerSystem.Glimmer++;
        }
    }
}
