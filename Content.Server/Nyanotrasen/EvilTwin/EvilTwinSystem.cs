using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Server.Humanoid;
using Content.Server.Station.Systems;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Spawners.Components;
using Content.Server.Psionics;
using Content.Server.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Server.GameObjects;

namespace Content.Server.EvilTwin
{
    public sealed class EvilTwinSystem : EntitySystem
    {
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;
        [Dependency] private readonly PsionicsSystem _psionicsSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EvilTwinSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        private void OnPlayerAttached(EntityUid uid, EvilTwinSpawnerComponent component, PlayerAttachedEvent args)
        {
            var twin = SpawnEvilTwin();

            if (twin != null)
            {
                args.Player.ContentData()?.Mind?.TransferTo(twin, true);
            }

            QueueDel(uid);
        }
        public EntityUid? SpawnEvilTwin()
        {
            var candidates = EntityQuery<ActorComponent, MindComponent, HumanoidComponent>().ToList();
            _random.Shuffle(candidates);

            foreach (var candidate in candidates)
            {
                var candUid = candidate.Item1.Owner;

                if (candidate.Item2.Mind?.CurrentJob == null)
                    continue;

                if (!_prototypeManager.TryIndex<SpeciesPrototype>(candidate.Item3.Species, out var species))
                    continue;

                var spawns = EntityQuery<SpawnPointComponent>();

                var coords = Transform(candidate.Item1.Owner).Coordinates;

                foreach (var spawn in spawns)
                {
                    if (spawn.SpawnType != SpawnPointType.LateJoin)
                        continue;

                    coords = Transform(spawn.Owner).Coordinates;
                    break;
                }

                var uid = Spawn(species.Prototype, coords);

                _humanoidSystem.CloneAppearance(candUid, uid);
                MetaData(uid).EntityName = MetaData(candUid).EntityName;

                if (candidate.Item2.Mind.CurrentJob.StartingGear != null && _prototypeManager.TryIndex<StartingGearPrototype>(candidate.Item2.Mind.CurrentJob.StartingGear, out var gear))
                    _stationSpawningSystem.EquipStartingGear(uid, gear, null);

                foreach (var special in candidate.Item2.Mind.CurrentJob.Prototype.Special)
                {
                    if (special is AddComponentSpecial)
                        special.AfterEquip(uid);
                }

                AddComp<EvilTwinComponent>(uid);
                var psi = EnsureComp<PotentialPsionicComponent>(uid);
                _psionicsSystem.RollPsionics(uid, psi, false, 100);

                return uid;
            }

            return null;
        }
    }
}
