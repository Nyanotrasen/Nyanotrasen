using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.NPC.Systems;
using Content.Server.NPC.Components;
using Content.Server.NPC;
using Content.Server.Pointing.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.RatKing
{
    public sealed class RatKingSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly TransformSystem _xform = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly FactionSystem _factionSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private const string NeutralAIFaction = "RatPassive";
        private const string HostileAIFaction = "RatHostile";

        private TimeSpan _nextRefresh = TimeSpan.FromSeconds(1.5);

        private TimeSpan _refreshTime = TimeSpan.FromSeconds(1.5);

        /// <summary>
        /// Why is following so bad that this is neccessary...
        /// </summary>
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_timing.CurTime >= _nextRefresh)
            {
                _nextRefresh = _timing.CurTime + _refreshTime;

                foreach (var servant in EntityQuery<RatServantComponent>())
                {
                    if (servant.RatKing == null)
                        continue;

                    _npc.SetBlackboard(servant.Owner, NPCBlackboard.FollowTarget, new EntityCoordinates(servant.RatKing.Value, Vector2.Zero));
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RatKingComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<RatKingComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<RatServantComponent, ComponentShutdown>(OnServantShutdown);

            SubscribeLocalEvent<RatKingComponent, PointedEvent>(OnPoint);

            SubscribeLocalEvent<RatKingComponent, RatKingRaiseArmyActionEvent>(OnRaiseArmy);
            SubscribeLocalEvent<RatKingComponent, RatKingDomainActionEvent>(OnDomain);
            SubscribeLocalEvent<RatKingComponent, RatKingToggleFactionActionEvent>(OnToggleFaction);
        }

        private void OnStartup(EntityUid uid, RatKingComponent component, ComponentStartup args)
        {
            _action.AddAction(uid, component.ActionToggleFaction, null);
            _action.AddAction(uid, component.ActionRaiseArmy, null);
            _action.AddAction(uid, component.ActionDomain, null);
        }

        private void OnMobStateChanged(EntityUid uid, RatKingComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == Shared.Mobs.MobState.Dead)
            {
                foreach (var servant in component.Servants)
                {
                    UpdateAIFaction(servant, true);
                }
            }
        }

        private void OnServantShutdown(EntityUid uid, RatServantComponent component, ComponentShutdown args)
        {
            if (!TryComp<RatKingComponent>(component.RatKing, out var king))
                return;

            king.Servants.Remove(uid);
        }

        /// <summary>
        /// This function is ON POINT.
        /// </summary>
        private void OnPoint(EntityUid uid, RatKingComponent component, ref PointedEvent args)
        {
            if (!HasComp<MobStateComponent>(args.Target))
                return;

            foreach (var servant in component.Servants)
            {
                var targeted = EnsureComp<NPCCombatTargetComponent>(servant);
                targeted.EngagingEnemies.Add(args.Target);
            }
        }


        /// <summary>
        /// Summons an allied rat servant at the King, costing a small amount of hunger
        /// </summary>
        private void OnRaiseArmy(EntityUid uid, RatKingComponent component, RatKingRaiseArmyActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            //make sure the hunger doesn't go into the negatives
            if (hunger.CurrentHunger < component.HungerPerArmyUse)
            {
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, uid);
                return;
            }
            args.Handled = true;
            hunger.CurrentHunger -= component.HungerPerArmyUse;
            var servant = Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates); //spawn the little mouse boi
            component.Servants.Add(servant);
            UpdateAIFaction(servant, component.HostileServants);

            var servComp = EnsureComp<RatServantComponent>(servant);
            servComp.RatKing = uid;

            var faction = EnsureComp<FactionComponent>(servant);
            _factionSystem.AddFriendlyEntity(servant, uid, faction);

            _npc.SetBlackboard(servant, NPCBlackboard.FollowTarget, new EntityCoordinates(uid, Vector2.Zero));
        }

        /// <summary>
        /// uses hunger to release a specific amount of miasma into the air. This heals the rat king
        /// and his servants through a specific metabolism.
        /// </summary>
        private void OnDomain(EntityUid uid, RatKingComponent component, RatKingDomainActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            //make sure the hunger doesn't go into the negatives
            if (hunger.CurrentHunger < component.HungerPerDomainUse)
            {
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, uid);
                return;
            }
            args.Handled = true;
            hunger.CurrentHunger -= component.HungerPerDomainUse;

            _popup.PopupEntity(Loc.GetString("rat-king-domain-popup"), uid);

            var transform = Transform(uid);
            var indices = _xform.GetGridOrMapTilePosition(uid, transform);
            var tileMix = _atmos.GetTileMixture(transform.GridUid, transform.MapUid, indices, true);
            tileMix?.AdjustMoles(Gas.Miasma, component.MolesMiasmaPerDomain);
        }

        private void OnToggleFaction(EntityUid uid, RatKingComponent component, RatKingToggleFactionActionEvent args)
        {
            component.HostileServants = !component.HostileServants;

            foreach (var servant in component.Servants)
            {
                UpdateAIFaction(servant, component.HostileServants);
            }
            UpdateAIFaction(uid, component.HostileServants);

            _action.SetToggled(component.ActionToggleFaction, component.HostileServants);
            args.Handled = true;
        }


        private void UpdateAIFaction(EntityUid servant, bool hostile, FactionComponent? component = null)
        {
            if (!Resolve(servant, ref component, false))
                return;

            if (hostile)
            {
                _factionSystem.RemoveFaction(servant, NeutralAIFaction);
                _factionSystem.AddFaction(servant, HostileAIFaction);
            } else
            {
                _factionSystem.RemoveFaction(servant, HostileAIFaction);
                _factionSystem.AddFaction(servant, NeutralAIFaction);
            }
        }
    }

    public sealed class RatKingRaiseArmyActionEvent : InstantActionEvent { };
    public sealed class RatKingDomainActionEvent : InstantActionEvent { };
    public sealed class RatKingToggleFactionActionEvent : InstantActionEvent { };
};
