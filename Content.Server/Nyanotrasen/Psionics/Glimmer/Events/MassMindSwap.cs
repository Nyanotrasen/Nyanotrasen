using Content.Shared.Mobs.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Mobs.Systems;
using Content.Server.Research.SophicScribe;
using Content.Server.Abilities.Psionics;

namespace Content.Server.Psionics.Glimmer;
/// <summary>
/// Zaps everyone, rolling psionics and disorienting them
/// </summary>
public sealed class MassMindSwap : GlimmerEventSystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly MindSwapPowerSystem _mindSwap = default!;

    public override string Prototype => "MassMindSwap";
    public override void Started()
    {
        base.Started();
        List<PotentialPsionicComponent> psionicList = new();
        foreach (var (psion, mobState) in EntityManager.EntityQuery<PotentialPsionicComponent, MobStateComponent>())
        {
            if (_mobStateSystem.IsAlive(psion.Owner) && !HasComp<PsionicInsulationComponent>(psion.Owner))
                psionicList.Add(psion);
        }

        // Even out with a scribe...
        if (psionicList.Count % 2 != 0)
        {
            foreach (var (psion, sophicScribe) in EntityManager.EntityQuery<PotentialPsionicComponent, SophicScribeComponent>())
            {
                psionicList.Add(psion);
                break;
            }
        }

        for (int i = 0; i < psionicList.Count; i += 2)
        {
            _mindSwap.Swap(psionicList[i].Owner, psionicList[i+1].Owner);
        }
    }
}
