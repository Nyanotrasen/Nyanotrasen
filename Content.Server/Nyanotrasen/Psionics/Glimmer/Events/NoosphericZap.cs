using Content.Shared.MobState.Components;
using Content.Shared.Abilities.Psionics;
using Content.Server.MobState;
using Content.Server.Electrocution;

namespace Content.Server.Psionics.Glimmer;
/// <summary>
/// Zaps everyone, rolling psionics and disorienting them
/// </summary>
public sealed class NoosphericZap : GlimmerEventSystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    public override string Prototype => "NoosphericZap";
    public override void Started()
    {
        base.Started();
        Logger.Error("Starting event...");
        List<PotentialPsionicComponent> psionicList = new();
        foreach (var (psion, mobState) in EntityManager.EntityQuery<PotentialPsionicComponent, MobStateComponent>())
        {
            if (_mobStateSystem.IsAlive(psion.Owner) && !HasComp<PsionicInsulationComponent>(psion.Owner))
                psionicList.Add(psion);
        }

        foreach (var psion in psionicList)
        {
            _electrocutionSystem.TryDoElectrocution(psion.Owner, null, 5, TimeSpan.FromSeconds(5), false, ignoreInsulation: true);
        }
        ForceEndSelf();
    }
}
