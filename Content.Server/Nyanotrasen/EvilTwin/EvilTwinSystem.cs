using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Preferences;
using Content.Server.Preferences.Managers;
using Content.Server.Humanoid;
using Content.Server.Station.Systems;
using Content.Server.Mind.Components;
using Content.Server.DetailExaminable;
using Content.Server.Players;
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

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EvilTwinSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<EvilTwinComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
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

        private void OnMindAdded(EntityUid uid, EvilTwinComponent component, MindAddedMessage args)
        {
            if (!TryComp<MindComponent>(uid, out var mindComponent) || mindComponent.Mind == null)
                return;

            var mind = mindComponent.Mind;

            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(EvilTwinRole)));
            mind.TryAddObjective(_prototypeManager.Index<ObjectivePrototype>(EscapeObjective));
            mind.TryAddObjective(_prototypeManager.Index<ObjectivePrototype>(KillObjective));
        }

        private void OnRoundEnd(RoundEndTextAppendEvent ev)
        {
            List<(EvilTwinComponent fugi, MindComponent mind)> fugis = EntityQuery<EvilTwinComponent, MindComponent>().ToList();

            if (fugis.Count < 1)
                return;

            var result = Loc.GetString("evil-twin-round-end-result", ("evil-twin-count", fugis.Count));

            // yeah this is duplicated from traitor rules lol, there needs to be a generic rewrite where it just goes through all minds with objectives
            foreach (var fugi in fugis)
            {
                if (fugi.mind.Mind == null)
                    continue;

                var name = fugi.mind.Mind.CharacterName;
                fugi.mind.Mind.TryGetSession(out var session);
                var username = session?.Name;

                var objectives = fugi.mind.Mind.AllObjectives.ToArray();
                if (objectives.Length == 0)
                {
                    if (username != null)
                    {
                        if (name == null)
                            result += "\n" + Loc.GetString("evil-twin-user-was-an-evil-twin", ("user", username));
                        else
                            result += "\n" + Loc.GetString("evil-twin-user-was-an-evil-twin-named", ("user", username), ("name", name));
                    }
                    else if (name != null)
                        result += "\n" + Loc.GetString("evil-twin-was-an-evil-twin-named", ("name", name));

                    continue;
                }

                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("evil-twin-user-was-an-evil-twin-with-objectives", ("user", username));
                    else
                        result += "\n" + Loc.GetString("evil-twin-user-was-an-evil-twin-with-objectives-named", ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("evil-twin-was-an-evil-twin-with-objectives-named", ("name", name));

                foreach (var objectiveGroup in objectives.GroupBy(o => o.Prototype.Issuer))
                {
                    foreach (var objective in objectiveGroup)
                    {
                        foreach (var condition in objective.Conditions)
                        {
                            var progress = condition.Progress;
                            if (progress > 0.99f)
                            {
                                result += "\n- " + Loc.GetString(
                                    "traitor-objective-condition-success",
                                    ("condition", condition.Title),
                                    ("markupColor", "green")
                                );
                            }
                            else
                            {
                                result += "\n- " + Loc.GetString(
                                    "traitor-objective-condition-fail",
                                    ("condition", condition.Title),
                                    ("progress", (int) (progress * 100)),
                                    ("markupColor", "red")
                                );
                            }
                        }
                    }
                }
            }
            ev.AddLine(result);
        }
        public EntityUid? SpawnEvilTwin()
        {
            var candidates = EntityQuery<ActorComponent, MindComponent, HumanoidAppearanceComponent>().ToList();
            _random.Shuffle(candidates);

            foreach (var candidate in candidates)
            {
                var candUid = candidate.Item1.Owner;

                if (candidate.Item2.Mind?.CurrentJob == null)
                    continue;

                if (HasComp<MetempsychosisKarmaComponent>(candUid))
                    continue;

                if (HasComp<FugitiveComponent>(candUid) || HasComp<EvilTwinComponent>(candUid) || HasComp<NukeOperativeComponent>(candUid))
                    continue;

                if (!_prototypeManager.TryIndex<SpeciesPrototype>(candidate.Item3.Species, out var species))
                    continue;

                var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(candidate.Item1.PlayerSession.UserId).SelectedCharacter;

                if (pref == null)
                    continue;

                var spawns = EntityQuery<SpawnPointComponent>();

                var coords = Transform(candUid).Coordinates;

                List<EntityUid> latejoins = new();

                foreach (var spawn in spawns)
                {
                    if (spawn.SpawnType != SpawnPointType.LateJoin)
                        continue;

                    latejoins.Add(spawn.Owner);

                    if (_stationSystem.GetOwningStation(spawn.Owner) !=_stationSystem.GetOwningStation(candUid))
                        continue;

                    coords = Transform(spawn.Owner).Coordinates;
                    break;
                }

                if (coords == Transform(candUid).Coordinates && latejoins.Count > 0)
                    coords = Transform(_random.Pick(latejoins)).Coordinates;

                var uid = Spawn(species.Prototype, coords);

                _humanoidSystem.LoadProfile(uid, pref);
                MetaData(uid).EntityName = MetaData(candUid).EntityName;

                if (TryComp<DetailExaminableComponent>(candUid, out var detail))
                {
                    var detailCopy = EnsureComp<DetailExaminableComponent>(uid);
                    detailCopy.Content = detail.Content;
                }

                if (candidate.Item2.Mind.CurrentJob.StartingGear != null && _prototypeManager.TryIndex<StartingGearPrototype>(candidate.Item2.Mind.CurrentJob.StartingGear, out var gear))
                {
                    _stationSpawningSystem.EquipStartingGear(uid, gear, pref);
                    _stationSpawningSystem.EquipIdCard(uid, pref.Name, candidate.Item2.Mind.CurrentJob.Prototype, _stationSystem.GetOwningStation(candUid));
                }

                foreach (var special in candidate.Item2.Mind.CurrentJob.Prototype.Special)
                {
                    if (special is AddComponentSpecial)
                        special.AfterEquip(uid);
                }

                var twin = EnsureComp<EvilTwinComponent>(uid);
                twin.TwinMind = candidate.Item2.Mind;

                var psi = EnsureComp<PotentialPsionicComponent>(uid);
                _psionicsSystem.RollPsionics(uid, psi, false, 100);

                return uid;
            }

            return null;
        }
    }
}
