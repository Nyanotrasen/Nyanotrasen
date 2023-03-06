using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Server.NPC.Components;
using Content.Server.Destructible;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems
{
    public sealed class NPCCombatTargetSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var engager in EntityQuery<NPCEngagerComponent>())
            {
                if (engager.RemoveWhen == null)
                    continue;

                if (_timing.CurTime < engager.RemoveWhen)
                    continue;

                RemCompDeferred<NPCEngagerComponent>(engager.Owner);
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NPCComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<NPCCombatTargetComponent, GetNearbyHostilesEvent>(OnAddHostiles);
            SubscribeLocalEvent<NPCEngagerComponent, ComponentShutdown>(OnShutdown);
        }

        public void StartHostility(EntityUid offended, EntityUid offender)
        {
            if (Deleted(offended) || Deleted(offender))
                return;

            var engaged = EnsureComp<NPCCombatTargetComponent>(offended);
            engaged.EngagingEnemies.Add(offender);

            var engager = EnsureComp<NPCEngagerComponent>(offender);
            engager.EngagedEnemies.Add(offended);

            engager.RemoveWhen = _timing.CurTime + engager.Decay;
        }

        private void OnDamageChanged(EntityUid uid, NPCComponent component, DamageChangedEvent args)
        {
            if (!args.DamageIncreased)
                return;

            if (args.Origin == null || args.Origin == uid)
                return;

            if (!HasComp<MobStateComponent>(args.Origin) && !HasComp<DestructibleComponent>(args.Origin))
                return;

            StartHostility(uid, args.Origin.Value);
        }

        private void OnAddHostiles(EntityUid uid, NPCCombatTargetComponent component, ref GetNearbyHostilesEvent args)
        {
            args.ExceptionalHostiles.UnionWith(component.EngagingEnemies);
        }
        private void OnShutdown(EntityUid uid, NPCEngagerComponent component, ComponentShutdown args)
        {
            foreach (var enemy in component.EngagedEnemies)
            {
                if (TryComp<NPCCombatTargetComponent>(enemy, out var targetComponent))
                {
                    targetComponent.EngagingEnemies.Remove(uid);

                    if (targetComponent.EngagingEnemies.Count == 0)
                        RemCompDeferred<NPCCombatTargetComponent>(enemy);
                }
            }
        }
    }
}
