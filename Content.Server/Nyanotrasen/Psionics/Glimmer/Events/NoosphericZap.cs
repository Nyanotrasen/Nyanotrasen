using Content.Shared.MobState.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared.StatusEffect;
using Content.Server.MobState;
using Content.Server.Stunnable;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.Psionics.Glimmer;
/// <summary>
/// Zaps everyone, rolling psionics and disorienting them
/// </summary>
public sealed class NoosphericZap : GlimmerEventSystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PsionicsSystem _psionicsSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override string Prototype => "NoosphericZap";
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
            _stunSystem.TryParalyze(psion.Owner, TimeSpan.FromSeconds(5), false);
            _statusEffectsSystem.TryAddStatusEffect(psion.Owner, "Stutter", TimeSpan.FromSeconds(10), false, "StutteringAccent");

            if (HasComp<PsionicComponent>(psion.Owner))
                _popupSystem.PopupEntity(Loc.GetString("noospheric-zap-seize"), psion.Owner, psion.Owner, Shared.Popups.PopupType.LargeCaution);
            else
            {
                if (psion.Rerolled)
                {
                    psion.Rerolled = false;
                    _popupSystem.PopupEntity(Loc.GetString("noospheric-zap-seize-potential-regained"), psion.Owner, psion.Owner, Shared.Popups.PopupType.LargeCaution);
                } else
                {
                    _psionicsSystem.RollPsionics(psion.Owner, psion, multiplier: 0.25f);
                    _popupSystem.PopupEntity(Loc.GetString("noospheric-zap-seize"), psion.Owner, psion.Owner, Shared.Popups.PopupType.LargeCaution);
                }
            }
        }
    }
}
