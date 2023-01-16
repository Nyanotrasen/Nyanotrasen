using Content.Shared.Mobs.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared.StatusEffect;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Robust.Shared.Player;

namespace Content.Server.Psionics.Glimmer;
/// <summary>
/// Mutes everyone for a random amount of time.
/// </summary>
public sealed class PsionicCatGotYourTongue : GlimmerEventSystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;

    public override string Prototype => "PsionicCatGotYourTongue";
    public override void Started()
    {
        base.Started();
        List<PotentialPsionicComponent> psionicList = new();
        foreach (var (psion, mobState) in EntityManager.EntityQuery<PotentialPsionicComponent, MobStateComponent>())
        {
            if (_mobStateSystem.IsAlive(psion.Owner) && !HasComp<PsionicInsulationComponent>(psion.Owner))
                psionicList.Add(psion);
        }

        foreach (var psion in psionicList)
        {
            _statusEffectsSystem.TryAddStatusEffect(psion.Owner, "Muted", TimeSpan.FromSeconds(_robustRandom.NextFloat(20, 80)), false, "Muted");
            _sharedAudioSystem.PlayGlobal("/Audio/Voice/Felinid/cat_scream1.ogg", Filter.Entities(psion.Owner), false);
        }
    }
}
