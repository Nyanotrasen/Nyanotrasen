using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.NPC.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;

namespace Content.Server.StationEvents;

/// <summary>
/// This rule makes hunger and thirst damage you if you're empty on either.
/// </summary>
[UsedImplicitly]
public sealed class DeathByStarvationRuleSystem : GameRuleSystem<DeathByStarvationRuleComponent>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    protected override void ActiveTick(EntityUid uid, DeathByStarvationRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        var curTime = _gameTiming.CurTime;

        if (curTime < component.NextTick)
            return;

        component.NextTick = curTime + component.TickInterval;

        var alert = Loc.GetString(component.Alert);

        var npcQuery = GetEntityQuery<NPCComponent>();

        var query = EntityQueryEnumerator<HungerComponent, ThirstComponent, DamageableComponent, MobStateComponent>();
        while (query.MoveNext(out var mob, out var hunger, out var thirst, out var damageable, out var mobState))
        {
            if (npcQuery.HasComponent(mob))
                // Skip NPCs. Remove this when we get smarter NPCs, maybe.
                //
                // An alternative design has a DeathByStarvation component,
                // but really that should be part of Hunger and Thirst.
                continue;

            if (_mobStateSystem.IsDead(mob, mobState))
                // Skip the dead.
                continue;

            if (hunger.CurrentThreshold != HungerThreshold.Dead &&
                thirst.CurrentThirstThreshold != ThirstThreshold.Dead)
            {
                // Skip those who are not yet starving or parched.
                continue;
            }

            var result = _damageableSystem.TryChangeDamage(mob, component.Damage, damageable: damageable);
            if (result?.Total <= 0)
                continue;

            if (_random.Prob(component.AlertProbability))
                _popupSystem.PopupEntity(alert, mob, mob, PopupType.MediumCaution);
        }
    }
}
