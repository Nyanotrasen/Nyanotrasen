using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Preferences;
using Content.Server.Preferences.Managers;
using Content.Server.Humanoid;
using Content.Server.Station.Systems;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.DetailExaminable;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Spawners.Components;
using Content.Server.Psionics;
using Content.Server.Jobs;
using Content.Server.Traitor;
using Content.Server.Objectives;
using Content.Server.GameTicking;
using Content.Server.Fugitive;
using Content.Server.Cloning;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.EvilTwin
{
    public sealed class EvilTwinSystem : EntitySystem
    {
        private const string EvilTwinRole = "EvilTwin";
        private const string KillObjective = "KillObjectiveEvilTwin";
        private const string EscapeObjective = "EscapeShuttleObjectiveEvilTwin";

        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly StationSpawningSystem _stationSpawningSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
        [Dependency] private readonly PsionicsSystem _psionicsSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IServerPreferencesManager _prefs = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EvilTwinSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<EvilTwinComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        }

        private void OnPlayerAttached(EntityUid uid, EvilTwinSpawnerComponent component, PlayerAttachedEvent args)
        {
            if (!TrySpawnParadoxAnomaly(out var twin))
                return;

            var mind = args.Player.ContentData()?.Mind;
            if (mind == null)
                return;

            _mindSystem.TransferTo(mind, twin, true);
            QueueDel(uid);
        }

        private void OnMindAdded(EntityUid uid, EvilTwinComponent component, MindAddedMessage args)
        {
            if (!_mindSystem.TryGetMind(uid, out var mind))
                return;

            if (component.FirstMindAdded)
                return;

            component.FirstMindAdded = true;

            _mindSystem.AddRole(mind, new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(EvilTwinRole)));
            _mindSystem.TryAddObjective(mind, _prototypeManager.Index<ObjectivePrototype>(EscapeObjective));
            _mindSystem.TryAddObjective(mind, _prototypeManager.Index<ObjectivePrototype>(KillObjective));
        }

        private void OnRoundEnd(RoundEndTextAppendEvent ev)
        {
            var result = new StringBuilder();
            var query = EntityQueryEnumerator<EvilTwinComponent, MindContainerComponent>();
            var count = 0;

            // yeah this is duplicated from traitor rules lol, there needs to be a generic rewrite where it just goes through all minds with objectives
            while (query.MoveNext(out var uid, out _, out var mindContainer))
            {
                if (!_mindSystem.TryGetMind(uid, out var mind, mindContainer))
                    continue;

                ++count;

                var name = mind.CharacterName;
                string? username = null;

                if (_mindSystem.TryGetSession(mind, out var session))
                    username = session.Name;

                var objectives = mind.AllObjectives.ToArray();
                if (objectives.Length == 0)
                {
                    if (username != null)
                    {
                        if (name == null)
                            result.AppendLine(Loc.GetString("evil-twin-user-was-an-evil-twin", ("user", username)));
                        else
                            result.AppendLine(Loc.GetString("evil-twin-user-was-an-evil-twin-named", ("user", username), ("name", name)));
                    }
                    else if (name != null)
                        result.AppendLine(Loc.GetString("evil-twin-was-an-evil-twin-named", ("name", name)));

                    continue;
                }

                if (username != null)
                {
                    if (name == null)
                        result.AppendLine(Loc.GetString("evil-twin-user-was-an-evil-twin-with-objectives", ("user", username)));
                    else
                        result.AppendLine(Loc.GetString("evil-twin-user-was-an-evil-twin-with-objectives-named", ("user", username), ("name", name)));
                }
                else if (name != null)
                    result.AppendLine(Loc.GetString("evil-twin-was-an-evil-twin-with-objectives-named", ("name", name)));

                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            if (progress > 0.99f)
                            {
                                result.AppendLine("- " + Loc.GetString(
                                    "traitor-objective-condition-success",
                                    ("condition", condition.Title),
                                    ("markupColor", "green")
                                ));
                            }
                            else
                            {
                                result.AppendLine("- " + Loc.GetString(
                                    "traitor-objective-condition-fail",
                                    ("condition", condition.Title),
                                    ("progress", (int) (progress * 100)),
                                    ("markupColor", "red")
                                ));
                            }
                        }
                    }
                }
            }

            if (count == 0)
                return;

            ev.AddLine(Loc.GetString("evil-twin-round-end-result", ("evil-twin-count", count)));
            ev.AddLine(result.ToString());
        }

        public bool TrySpawnParadoxAnomaly([NotNullWhen(true)] out EntityUid? twin, EntityUid? specific = null)
        {
            twin = null;

            // Get a list of potential candidates if one hasn't been specified.
            var candidates = new List<(EntityUid uid, Mind.Mind mind, Job job, SpeciesPrototype species, HumanoidCharacterProfile profile)>();

            if (specific == null)
            {
                var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent>();
                while (query.MoveNext(out var uid, out var mindContainer, out var humanoidAppearance))
                {
                    if (!_mindSystem.TryGetMind(uid, out var mind, mindContainer) ||
                        mind.CurrentJob == null)
                    {
                        continue;
                    }

                    if (humanoidAppearance.LastProfileLoaded == null)
                        continue;

                    if (HasComp<MetempsychosisKarmaComponent>(uid) ||
                        HasComp<FugitiveComponent>(uid) ||
                        HasComp<EvilTwinComponent>(uid) ||
                        HasComp<NukeOperativeComponent>(uid))
                    {
                        continue;
                    }

                    if (!_prototypeManager.TryIndex<SpeciesPrototype>(humanoidAppearance.Species, out var species))
                    {
                        continue;
                    }

                    candidates.Add((uid, mind, mind.CurrentJob, species, humanoidAppearance.LastProfileLoaded));
                }
            }
            else
            {
                if (!_mindSystem.TryGetMind(specific.Value, out var mind) ||
                    mind.CurrentJob == null ||
                    !TryComp<HumanoidAppearanceComponent>(specific, out var humanoidAppearance) ||
                    humanoidAppearance.LastProfileLoaded == null ||
                    !_prototypeManager.TryIndex<SpeciesPrototype>(humanoidAppearance.Species, out var species))
                {
                    return false;
                }

                candidates.Add((specific.Value, mind, mind.CurrentJob, species, humanoidAppearance.LastProfileLoaded));
            }

            // Select a candidate.
            if (candidates.Count == 0)
            {
                return false;
            }

            var (candidate, candidateMind, candidateJob, candidateSpecies, candidateProfile) = _random.Pick(candidates);

            // Find a suitable spawn point.
            var coords = Transform(candidate).Coordinates;

            var latejoins = new List<EntityUid>();

            var spawnQuery = EntityQueryEnumerator<SpawnPointComponent>();
            while (spawnQuery.MoveNext(out var uid, out var spawnPoint))
            {
                if (spawnPoint.SpawnType != SpawnPointType.LateJoin)
                    continue;

                if (_stationSystem.GetOwningStation(uid) !=_stationSystem.GetOwningStation(candidate))
                    continue;

                latejoins.Add(uid);
            }

            if (latejoins.Count == 0)
            {
                return false;
            }

            // Spawn the twin.
            var destination = Transform(_random.Pick(latejoins)).Coordinates;
            var spawned = Spawn(candidateSpecies.Prototype, destination);

            // Copy the details.
            _humanoidSystem.LoadProfile(spawned, candidateProfile);
            _metaDataSystem.SetEntityName(spawned, MetaData(candidate).EntityName);

            if (TryComp<DetailExaminableComponent>(candidate, out var detail))
            {
                var detailCopy = EnsureComp<DetailExaminableComponent>(spawned);
                detailCopy.Content = detail.Content;
            }

            if (candidateJob.StartingGear != null &&
                _prototypeManager.TryIndex<StartingGearPrototype>(candidateJob.StartingGear, out var gear))
            {
                _stationSpawningSystem.EquipStartingGear(spawned, gear, candidateProfile);
                _stationSpawningSystem.EquipIdCard(spawned,
                    candidateProfile.Name,
                    candidateJob.Prototype,
                    _stationSystem.GetOwningStation(candidate));
            }

            foreach (var special in candidateJob.Prototype.Special)
            {
                if (special is AddComponentSpecial)
                    special.AfterEquip(spawned);
            }

            var twinComponent = EnsureComp<EvilTwinComponent>(spawned);
            twinComponent.TwinMind = candidateMind;

            var psi = EnsureComp<PotentialPsionicComponent>(spawned);
            _psionicsSystem.RollPsionics(spawned, psi, false, 100);

            twin = spawned;
            return true;
        }
    }
}
