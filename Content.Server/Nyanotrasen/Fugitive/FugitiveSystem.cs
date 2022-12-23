using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Traitor;
using Content.Server.Objectives;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Paper;
using Content.Server.Humanoid;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Server.Ghost.Components;
using Content.Server.Roles;
using Content.Server.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Movement.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Random.Helpers;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using static Content.Shared.Examine.ExamineSystemShared;

namespace Content.Server.Fugitive
{
    public sealed class FugitiveSystem : EntitySystem
    {
        private const string FugitiveRole = "Fugitive";
        private const string EscapeObjective = "EscapeShuttleObjectiveFugitive";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly StunSystem _stun = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FugitiveComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
            SubscribeLocalEvent<FugitiveComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (cd, _) in EntityQuery<FugitiveCountdownComponent, FugitiveComponent>())
            {
                if (cd.AnnounceTime != null && _timing.CurTime > cd.AnnounceTime)
                {
                    _chat.DispatchGlobalAnnouncement(Loc.GetString("station-event-fugitive-hunt-announcement"), sender: Loc.GetString("fugitive-announcement-GALPOL"), colorOverride: Color.Yellow);

                    foreach (var console in EntityQuery<CommunicationsConsoleComponent>())
                    {
                        if (HasComp<GhostComponent>(console.Owner))
                            continue;

                        var paperEnt = Spawn("Paper", Transform(console.Owner).Coordinates);

                        MetaData(paperEnt).EntityName = Loc.GetString("fugi-report-ent-name", ("name", cd.Owner));

                        if (!TryComp<PaperComponent>(paperEnt, out var paper))
                            continue;

                        var report = GenerateFugiReport(cd.Owner);

                        _paperSystem.SetContent(paperEnt, report.ToMarkup(), paper);
                    }

                    RemCompDeferred<FugitiveCountdownComponent>(cd.Owner);
                }
            }
        }

        private void OnSpawned(EntityUid uid, FugitiveComponent component, GhostRoleSpawnerUsedEvent args)
        {
            if (TryComp<FugitiveCountdownComponent>(uid, out var cd))
                cd.AnnounceTime = _timing.CurTime + cd.AnnounceCD;

            _popupSystem.PopupEntity(Loc.GetString("fugitive-spawn", ("name", uid)), uid,
            Filter.Pvs(uid).RemoveWhereAttachedEntity(entity => !ExamineSystemShared.InRangeUnOccluded(uid, entity, ExamineRange, null)), true, Shared.Popups.PopupType.LargeCaution);

            _stun.TryParalyze(uid, TimeSpan.FromSeconds(2), false);
            _audioSystem.PlayPvs(component.SpawnSoundPath, uid, AudioParams.Default.WithVolume(-6f));

            var tile = Spawn("FloorTileItemSteel", Transform(uid).Coordinates);
            tile.RandomOffset(0.3f);
        }

        private void OnMindAdded(EntityUid uid, FugitiveComponent component, MindAddedMessage args)
        {
            if (!TryComp<MindComponent>(uid, out var mindComponent) || mindComponent.Mind == null)
                return;

            var mind = mindComponent.Mind;

            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(FugitiveRole)));

            mind.TryAddObjective(_prototypeManager.Index<ObjectivePrototype>(EscapeObjective));

            if (_prototypeManager.TryIndex<JobPrototype>("Fugitive", out var fugitive))
                mind.AddRole(new Job(mind, fugitive));

            // workaround seperate shitcode moment
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }

        private void OnRoundEnd(RoundEndTextAppendEvent ev)
        {
            List<(FugitiveComponent fugi, MindComponent mind)> fugis = EntityQuery<FugitiveComponent, MindComponent>().ToList();

            if (fugis.Count < 1)
                return;

            var result = Loc.GetString("fugitive-round-end-result", ("fugitiveCount", fugis.Count));

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
                            result += "\n" + Loc.GetString("fugitive-user-was-a-fugitive", ("user", username));
                        else
                            result += "\n" + Loc.GetString("fugitive-user-was-a-fugitive-named", ("user", username), ("name", name));
                    }
                    else if (name != null)
                        result += "\n" + Loc.GetString("fugitive-was-a-fugitive-named", ("name", name));

                    continue;
                }

                if (username != null)
                {
                    if (name == null)
                        result += "\n" + Loc.GetString("fugitive-user-was-a-fugitive-with-objectives", ("user", username));
                    else
                        result += "\n" + Loc.GetString("fugitive-user-was-a-fugitive-with-objectives-named", ("user", username), ("name", name));
                }
                else if (name != null)
                    result += "\n" + Loc.GetString("fugitive-was-a-fugitive-with-objectives-named", ("name", name));

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

        private FormattedMessage GenerateFugiReport(EntityUid uid)
        {
            FormattedMessage report = new();
            report.AddMarkup(Loc.GetString("fugi-report-title", ("name", uid)));
            report.PushNewline();
            report.PushNewline();
            report.AddMarkup(Loc.GetString("fugitive-report-first-line", ("name", uid)));
            report.PushNewline();
            report.PushNewline();


            if (!TryComp<HumanoidComponent>(uid, out var humanoidComponent) ||
                !_prototypeManager.TryIndex<SpeciesPrototype>(humanoidComponent.Species, out var species))
            {
                report.AddMarkup(Loc.GetString("fugitive-report-inhuman", ("name", uid)));
                return report;
            }

            report.AddMarkup(Loc.GetString("fugitive-report-morphotype", ("species", Loc.GetString(species.Name))));
            report.PushNewline();
            report.AddMarkup(Loc.GetString("fugitive-report-age", ("age", humanoidComponent.Age)));
            report.PushNewline();

            string sexLine = string.Empty;
            sexLine += humanoidComponent.Sex switch
            {
                Sex.Male => Loc.GetString("fugitive-report-sex-m"),
                Sex.Female => Loc.GetString("fugitive-report-sex-f"),
                _ => Loc.GetString("fugitive-report-sex-n")
            };

            report.AddMarkup(sexLine);

            if (TryComp<PhysicsComponent>(uid, out var physics))
            {
                report.PushNewline();
                report.AddMarkup(Loc.GetString("fugitive-report-weight", ("weight", Math.Round(physics.FixturesMass))));
            }
            report.PushNewline();
            report.PushNewline();
            report.AddMarkup(Loc.GetString("fugitive-report-last-line"));

            return report;
        }
    }
}
