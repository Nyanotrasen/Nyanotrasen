using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Server.Humanoid;
using Content.Server.Station.Systems;
using Content.Server.Mind.Components;
using Content.Server.Jobs;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;

namespace Content.Server.EvilTwin
{
    public sealed class EvilTwinSystem : EntitySystem
    {
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly HumanoidSystem _humanoidSystem = default!;

        public EntityUid? SpawnEvilTwin()
        {
            var candidates = EntityQuery<ActorComponent, MindComponent, HumanoidComponent>();

            foreach (var candidate in candidates)
            {
                var candUid = candidate.Item1.Owner;

                if (candidate.Item2.Mind?.CurrentJob == null)
                    continue;

                if (!_prototypeManager.TryIndex<SpeciesPrototype>(candidate.Item3.Species, out var species))
                    continue;

                var uid = Spawn(species.Prototype, Transform(candUid).Coordinates);

                _humanoidSystem.CloneAppearance(candUid, uid);
                MetaData(uid).EntityName = MetaData(candUid).EntityName;

                if (candidate.Item2.Mind.CurrentJob.StartingGear != null && _prototypeManager.TryIndex<StartingGearPrototype>(candidate.Item2.Mind.CurrentJob.StartingGear, out var gear))
                    _stationSpawningSystem.EquipStartingGear(uid, gear, null);

                foreach (var special in candidate.Item2.Mind.CurrentJob.Prototype.Special)
                {
                    if (special is AddComponentSpecial)
                        special.AfterEquip(uid);
                }

                return uid;
            }

            return null;
        }
    }
}
