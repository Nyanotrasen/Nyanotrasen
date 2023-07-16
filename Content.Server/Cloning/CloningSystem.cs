using Content.Shared.GameTicking;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Cloning;
using Content.Shared.Speech;
using Content.Shared.Atmos;
using Content.Shared.Tag;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Emoting;
using Content.Server.Psionics;
using Content.Server.Cloning.Components;
using Content.Server.Speech.Components;
using Content.Server.Mind.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Server.EUI;
using Content.Server.Humanoid;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Construction;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Materials;
using Content.Server.Jobs;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Content.Server.Mind;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Zombies;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.GameObjects.Components.Localization;
using Content.Server.Traits.Assorted;

namespace Content.Server.Cloning
{
    public sealed class CloningSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = null!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly EuiManager _euiManager = null!;
        [Dependency] private readonly CloningConsoleSystem _cloningConsoleSystem = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly PuddleSystem _puddleSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly MaterialStorageSystem _material = default!;
        [Dependency] private readonly MetempsychoticMachineSystem _metem = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly IServerPreferencesManager _prefs = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;

        public readonly Dictionary<Mind.Mind, EntityUid> ClonesWaitingForMind = new();
        public const float EasyModeCloningCost = 0.7f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CloningPodComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<CloningPodComponent, RefreshPartsEvent>(OnPartsRefreshed);
            SubscribeLocalEvent<CloningPodComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<BeingClonedComponent, MindAddedMessage>(HandleMindAdded);
            SubscribeLocalEvent<CloningPodComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<CloningPodComponent, AnchorStateChangedEvent>(OnAnchor);
            SubscribeLocalEvent<CloningPodComponent, ExaminedEvent>(OnExamined);
        }

        private void OnComponentInit(EntityUid uid, CloningPodComponent clonePod, ComponentInit args)
        {
            clonePod.BodyContainer = _containerSystem.EnsureContainer<ContainerSlot>(uid, "clonepod-bodyContainer");
            _signalSystem.EnsureSinkPorts(uid, CloningPodComponent.PodPort);
        }

        private void OnPartsRefreshed(EntityUid uid, CloningPodComponent component, RefreshPartsEvent args)
        {
            var materialRating = args.PartRatings[component.MachinePartMaterialUse];
            var speedRating = args.PartRatings[component.MachinePartCloningSpeed];

            component.BiomassRequirementMultiplier = MathF.Pow(component.PartRatingMaterialMultiplier, materialRating - 1);
            component.CloningTime = component.BaseCloningTime * MathF.Pow(component.PartRatingSpeedMultiplier, speedRating - 1);
        }

        private void OnUpgradeExamine(EntityUid uid, CloningPodComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("cloning-pod-component-upgrade-speed", component.BaseCloningTime / component.CloningTime);
            args.AddPercentageUpgrade("cloning-pod-component-upgrade-biomass-requirement", component.BiomassRequirementMultiplier);
        }

        internal void TransferMindToClone(Mind.Mind mind)
        {
            if (!ClonesWaitingForMind.TryGetValue(mind, out var entity) ||
                !EntityManager.EntityExists(entity) ||
                !TryComp<MindContainerComponent>(entity, out var mindComp) ||
                mindComp.Mind != null)
                return;

            _mindSystem.TransferTo(mind, entity, ghostCheckOverride: true);
            _mindSystem.UnVisit(mind);
            ClonesWaitingForMind.Remove(mind);
        }

        private void HandleMindAdded(EntityUid uid, BeingClonedComponent clonedComponent, MindAddedMessage message)
        {
            if (clonedComponent.Parent == EntityUid.Invalid ||
                !EntityManager.EntityExists(clonedComponent.Parent) ||
                !TryComp<CloningPodComponent>(clonedComponent.Parent, out var cloningPodComponent) ||
                uid != cloningPodComponent.BodyContainer.ContainedEntity)
            {
                EntityManager.RemoveComponent<BeingClonedComponent>(uid);
                return;
            }
            UpdateStatus(clonedComponent.Parent, CloningPodStatus.Cloning, cloningPodComponent);
        }

        private void OnPortDisconnected(EntityUid uid, CloningPodComponent pod, PortDisconnectedEvent args)
        {
            pod.ConnectedConsole = null;
        }

        private void OnAnchor(EntityUid uid, CloningPodComponent component, ref AnchorStateChangedEvent args)
        {
            if (component.ConnectedConsole == null || !TryComp<CloningConsoleComponent>(component.ConnectedConsole, out var console))
                return;

            if (args.Anchored)
            {
                _cloningConsoleSystem.RecheckConnections(component.ConnectedConsole.Value, uid, console.GeneticScanner, console);
                return;
            }
            _cloningConsoleSystem.UpdateUserInterface(component.ConnectedConsole.Value, console);
        }

        private void OnExamined(EntityUid uid, CloningPodComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange || !_powerReceiverSystem.IsPowered(uid))
                return;

            args.PushMarkup(Loc.GetString("cloning-pod-biomass", ("number", _material.GetMaterialAmount(uid, component.RequiredMaterial))));
        }

        public bool TryCloning(EntityUid uid, EntityUid bodyToClone, Mind.Mind mind, CloningPodComponent? clonePod, float failChanceModifier = 1, float karmaBonus = 0.25f)
        {
            if (!Resolve(uid, ref clonePod))
                return false;

            if (HasComp<ActiveCloningPodComponent>(uid))
                return false;

            if (ClonesWaitingForMind.TryGetValue(mind, out var clone))
            {
                if (EntityManager.EntityExists(clone) &&
                    !_mobStateSystem.IsDead(clone) &&
                    TryComp<MindContainerComponent>(clone, out var cloneMindComp) &&
                    (cloneMindComp.Mind == null || cloneMindComp.Mind == mind))
                    return false; // Mind already has clone

                ClonesWaitingForMind.Remove(mind);
            }

            if (mind.OwnedEntity != null && !_mobStateSystem.IsDead(mind.OwnedEntity.Value))
                return false; // Body controlled by mind is not dead

            // Yes, we still need to track down the client because we need to open the Eui
            if (mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out var client))
                return false; // If we can't track down the client, we can't offer transfer. That'd be quite bad.

            if (!TryComp<HumanoidAppearanceComponent>(bodyToClone, out var humanoid))
                return false; // whatever body was to be cloned, was not a humanoid

            // Begin Nyano-code: allow paradox anomalies to be cloned.
            var pref = humanoid.LastProfileLoaded;
            // End Nyano-code.

            if (pref == null)
                return false;

            if (!_prototype.TryIndex<SpeciesPrototype>(humanoid.Species, out var speciesPrototype))
                return false;

            if (!TryComp<PhysicsComponent>(bodyToClone, out var physics))
                return false;

            var cloningCost = clonePod.ConstantBiomassCost == null ? (int) Math.Round(physics.FixturesMass * clonePod.BiomassRequirementMultiplier) : (int) Math.Round((float) clonePod.ConstantBiomassCost * clonePod.BiomassRequirementMultiplier);

            if (clonePod.ConstantBiomassCost == null && _configManager.GetCVar(CCVars.BiomassEasyMode))
                cloningCost = (int) Math.Round(cloningCost * EasyModeCloningCost);

            // Check if they have the uncloneable trait
            if (TryComp<UncloneableComponent>(bodyToClone, out _))
            {
                if (clonePod.ConnectedConsole != null)
                    _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value,
                        Loc.GetString("cloning-console-uncloneable-trait-error"),
                        InGameICChatType.Speak, false);
                return false;
            }

            // biomass checks
            var biomassAmount = _material.GetMaterialAmount(uid, clonePod.RequiredMaterial);

            if (biomassAmount < cloningCost)
            {
                if (clonePod.ConnectedConsole != null)
                    _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-chat-error", ("units", cloningCost)), InGameICChatType.Speak, false);
                return false;
            }

            _material.TryChangeMaterialAmount(uid, clonePod.RequiredMaterial, -cloningCost);
            clonePod.UsedBiomass = cloningCost;
            // end of biomass checks

            // genetic damage checks
            if (clonePod.CheckGeneticDamage)
            {
                if (TryComp<DamageableComponent>(bodyToClone, out var damageable) &&
                    damageable.Damage.DamageDict.TryGetValue("Cellular", out var cellularDmg))
                {
                    var chance = Math.Clamp((float) (cellularDmg / 100), 0, 1);
                    chance *= failChanceModifier;

                    if (cellularDmg > 0 && clonePod.ConnectedConsole != null)
                        _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-cellular-warning", ("percent", Math.Round(100 - (chance * 100)))), InGameICChatType.Speak, false);

                    if (_robustRandom.Prob(chance))
                    {
                        UpdateStatus(uid, CloningPodStatus.Gore, clonePod);
                        clonePod.FailedClone = true;
                        AddComp<ActiveCloningPodComponent>(uid);
                        return true;
                    }
                // Begin Nyano-code:
                // arachne have no genetics. we need to stop the cloner from cloning them.
                }
                else
                {
                    if (clonePod.ConnectedConsole != null)
                        _chatSystem.TrySendInGameICMessage(clonePod.ConnectedConsole.Value, Loc.GetString("cloning-console-chat-no-genetics", ("units", cloningCost)), InGameICChatType.Speak, false);
                    return false;
                }
                // End Nyano-code.
            }
            // end of genetic damage checks

            var mob = FetchAndSpawnMob(clonePod, pref, speciesPrototype, humanoid, bodyToClone, karmaBonus);

            var cloneMindReturn = EntityManager.AddComponent<BeingClonedComponent>(mob);
            cloneMindReturn.Mind = mind;
            cloneMindReturn.Parent = uid;
            clonePod.BodyContainer.Insert(mob);
            ClonesWaitingForMind.Add(mind, mob);
            UpdateStatus(uid, CloningPodStatus.NoMind, clonePod);
            _euiManager.OpenEui(new AcceptCloningEui(mind, this), client);

            AddComp<ActiveCloningPodComponent>(uid);

            // TODO: Ideally, components like this should be on a mind entity so this isn't neccesary.
            // Remove this when 'mind entities' are added.
            // Add on special job components to the mob.
            if (mind.CurrentJob != null)
            {
                foreach (var special in mind.CurrentJob.Prototype.Special)
                {
                    if (special is AddComponentSpecial)
                        special.AfterEquip(mob);
                }
            }

            return true;
        }

        public void UpdateStatus(EntityUid podUid, CloningPodStatus status, CloningPodComponent cloningPod)
        {
            cloningPod.Status = status;
            _appearance.SetData(podUid, CloningPodVisuals.Status, cloningPod.Status);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<ActiveCloningPodComponent, CloningPodComponent>();
            while (query.MoveNext(out var uid, out var _, out var cloning))
            {
                if (!_powerReceiverSystem.IsPowered(uid))
                    continue;

                if (cloning.BodyContainer.ContainedEntity == null && !cloning.FailedClone)
                    continue;

                cloning.CloningProgress += frameTime;
                if (cloning.CloningProgress < cloning.CloningTime)
                    continue;

                if (cloning.FailedClone)
                    EndFailedCloning(uid, cloning);
                else
                    Eject(uid, cloning);
            }
        }

        public void Eject(EntityUid uid, CloningPodComponent? clonePod)
        {
            if (!Resolve(uid, ref clonePod))
                return;

            if (clonePod.BodyContainer.ContainedEntity is not { Valid: true } entity || clonePod.CloningProgress < clonePod.CloningTime)
                return;

            EntityManager.RemoveComponent<BeingClonedComponent>(entity);
            clonePod.BodyContainer.Remove(entity);
            clonePod.CloningProgress = 0f;
            clonePod.UsedBiomass = 0;
            UpdateStatus(uid, CloningPodStatus.Idle, clonePod);
            RemCompDeferred<ActiveCloningPodComponent>(uid);
        }

        private void EndFailedCloning(EntityUid uid, CloningPodComponent clonePod)
        {
            clonePod.FailedClone = false;
            clonePod.CloningProgress = 0f;
            UpdateStatus(uid, CloningPodStatus.Idle, clonePod);
            var transform = Transform(uid);
            var indices = _transformSystem.GetGridOrMapTilePosition(uid);

            var tileMix = _atmosphereSystem.GetTileMixture(transform.GridUid, null, indices, true);

            Solution bloodSolution = new();

            var i = 0;
            while (i < 1)
            {
                tileMix?.AdjustMoles(Gas.Miasma, 6f);
                bloodSolution.AddReagent("Blood", 50);
                if (_robustRandom.Prob(0.2f))
                    i++;
            }
            _puddleSystem.TrySpillAt(uid, bloodSolution, out _);

            _material.SpawnMultipleFromMaterial(_robustRandom.Next(1, (int) (clonePod.UsedBiomass / 2.5)), clonePod.RequiredMaterial, Transform(uid).Coordinates);

            clonePod.UsedBiomass = 0;
            RemCompDeferred<ActiveCloningPodComponent>(uid);
        }

        /// <summary>
        /// Handles fetching the mob and any appearance stuff...
        /// </summary>
        private EntityUid FetchAndSpawnMob(CloningPodComponent clonePod, HumanoidCharacterProfile pref, SpeciesPrototype speciesPrototype, HumanoidAppearanceComponent humanoid, EntityUid bodyToClone, float karmaBonus)
        {
            List<Sex> sexes = new();
            bool switchingSpecies = false;
            bool applyKarma = false;
            var name = pref.Name;
            var toSpawn = speciesPrototype.Prototype;
            TryComp<MetempsychosisKarmaComponent>(bodyToClone, out var oldKarma);

            if (TryComp<MetempsychoticMachineComponent>(clonePod.Owner, out var metem))
            {
                toSpawn = _metem.GetSpawnEntity(clonePod.Owner, karmaBonus, speciesPrototype, out var newSpecies, oldKarma?.Score, metem);
                applyKarma = true;

                if (newSpecies != null)
                {
                    sexes = newSpecies.Sexes;

                    if (speciesPrototype.ID != newSpecies.ID)
                    {
                        switchingSpecies = true;
                    }

                    speciesPrototype = newSpecies;
                }
            }

            var mob = Spawn(toSpawn, Transform(clonePod.Owner).MapPosition);
            if (TryComp<HumanoidAppearanceComponent>(mob, out var newHumanoid))
            {
                if (switchingSpecies || HasComp<MetempsychosisKarmaComponent>(bodyToClone))
                {
                    pref = HumanoidCharacterProfile.RandomWithSpecies(newHumanoid.Species);
                    if (sexes.Contains(humanoid.Sex))
                        pref = pref.WithSex(humanoid.Sex);

                    pref = pref.WithGender(humanoid.Gender);
                    pref = pref.WithAge(humanoid.Age);

                }
                _humanoidSystem.LoadProfile(mob, pref);
            }

            if (applyKarma)
            {
                var karma = EnsureComp<MetempsychosisKarmaComponent>(mob);
                karma.Score++;
                if (oldKarma != null)
                    karma.Score += oldKarma.Score;
            }

            var ev = new CloningEvent(bodyToClone, mob);
            RaiseLocalEvent(bodyToClone, ref ev);

            if (!ev.NameHandled)
                MetaData(mob).EntityName = name;

            var mind = EnsureComp<MindContainerComponent>(mob);
            _mindSystem.SetExamineInfo(mob, true, mind);

            var grammar = EnsureComp<GrammarComponent>(mob);
            grammar.ProperNoun = true;
            grammar.Gender = humanoid.Gender;
            Dirty(grammar);

            EnsureComp<PotentialPsionicComponent>(mob);
            EnsureComp<SpeechComponent>(mob);
            EnsureComp<EmotingComponent>(mob);
            RemComp<ReplacementAccentComponent>(mob);
            RemComp<MonkeyAccentComponent>(mob);
            RemComp<SentienceTargetComponent>(mob);
            RemComp<GhostTakeoverAvailableComponent>(mob);

            _tag.AddTag(mob, "DoorBumpOpener");

            return mob;
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            ClonesWaitingForMind.Clear();
        }
    }

    /// <summary>
    /// Raised after a new mob got spawned when cloning a humanoid
    /// </summary>
    [ByRefEvent]
    public struct CloningEvent
    {
        public bool NameHandled = false;

        public readonly EntityUid Source;
        public readonly EntityUid Target;

        public CloningEvent(EntityUid source, EntityUid target)
        {
            Source = source;
            Target = target;
        }
    }
}
