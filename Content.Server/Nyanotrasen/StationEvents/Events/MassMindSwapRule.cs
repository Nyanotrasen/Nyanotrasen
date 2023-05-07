using Content.Server.Abilities.Psionics;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Psionics;
using Content.Server.Research.SophicScribe;
using Content.Server.StationEvents.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Forces a mind swap on all non-insulated potential psionic entities.
/// </summary>
internal sealed class MassMindSwapRule : StationEventSystem<MassMindSwapRuleComponent>
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly MindSwapPowerSystem _mindSwap = default!;

    protected override void Started(EntityUid uid, MassMindSwapRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        List<EntityUid> psionicList = new();

        var query = EntityQueryEnumerator<PotentialPsionicComponent, MobStateComponent>();
        while (query.MoveNext(out var psion, out _, out _))
        {
            if (_mobStateSystem.IsAlive(psion) && !HasComp<PsionicInsulationComponent>(psion))
                psionicList.Add(psion);
        }

        // Even out with a scribe...
        if (psionicList.Count % 2 != 0)
        {
            var queryScribe = EntityQueryEnumerator<SophicScribeComponent>();
            while (queryScribe.MoveNext(out var scribe, out _))
            {
                psionicList.Add(scribe);
                break;
            }
        }

        for (int i = 0; i < psionicList.Count; i += 2)
        {
            var performer = psionicList[i];
            var target = psionicList[i+1];

            _mindSwap.Swap(performer, target);

            if (!component.IsTemporary)
            {
                _mindSwap.GetTrapped(performer);
                _mindSwap.GetTrapped(target);
            }
        }
    }
}
