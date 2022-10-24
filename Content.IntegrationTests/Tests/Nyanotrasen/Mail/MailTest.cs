using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mail;
using Content.Server.Access.Systems;
using Content.Server.Mail;
using Content.Server.Mail.Components;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(MailSystem))]
    [TestOf(typeof(MailComponent))]
    [TestOf(typeof(MailTeleporterComponent))]
    public sealed class MailTest : EntitySystem
    {
        private const string Prototypes = @"
- type: entity
  id: HumanDummy
  name: HumanDummy
  components:
  - type: Hands
  - type: Body
    template: HumanoidTemplate
    preset: HumanPreset
    centerSlot: torso
  - type: MailReceiver

- type: entity
  id: TestMailTeleporter
  parent: BaseStructureDynamic
  name: TestMailTeleporter
  components:
  - type: MailTeleporter
    priorityChance: 0
    fragileBonus: 1
    fragileMalus: -1

- type: entity
  id: TestMailTeleporterAlwaysPriority
  parent: BaseStructureDynamic
  name: TestMailTeleporterAlwaysPriority
  components:
  - type: MailTeleporter
    priorityChance: 1

- type: entity
  id: TestMailTeleporterAlwaysPriorityAlwaysBrutal
  parent: BaseStructureDynamic
  name: TestMailTeleporterAlwaysPriorityAlwaysBrutal
  components:
  - type: MailTeleporter
    priorityChance: 1
    priorityDuration: 0.001

- type: entity
  id: TestMailTeleporterAlwaysOneAtATime
  parent: BaseStructureDynamic
  name: TestMailTeleporterAlwaysOneAtATime
  components:
  - type: MailTeleporter
    maximumUndeliveredParcels: 1

- type: entity
  parent: BaseMail
  id: TestMail
  name: TestMail

- type: entity
  parent: BaseMail
  id: TestMailFragileDetection
  name: TestMailFragileDetection
  components:
  - type: Mail
    contents:
    - id: DrinkGlass

- type: entity
  parent: BaseMail
  id: TestMailPriorityOnSpawn
  name: TestMailPriorityOnSpawn
  components:
  - type: Mail
    isPriority: true

- type: entity
  parent: BaseMail
  id: TestMailFragileOnSpawn
  name: TestMailFragileOnSpawn
  components:
  - type: Mail
    isFragile: true
";

        [Test]
        public async Task TestTeleporterCanSpawnPriorityMail()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporterAlwaysPriority", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);

                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "passenger", new HashSet<string>());

                var mailComponent = entityManager.GetComponent<MailComponent>(mail);

                Assert.IsTrue(mailComponent.IsPriority,
                    $"MailTeleporter failed to spawn priority mail when PriorityChance set to 1");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestPriorityBonusMalus()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);
                EntityUid mailPriority = entityManager.SpawnEntity("TestMailPriorityOnSpawn", coordinates);

                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "passenger", new HashSet<string>());
                mailSystem.SetupMail(mailPriority, teleporterComponent, "Bob", "passenger", new HashSet<string>());

                var mailComponent = entityManager.GetComponent<MailComponent>(mail);
                var mailPriorityComponent = entityManager.GetComponent<MailComponent>(mailPriority);

                var expectedBounty = teleporterComponent.PriorityBonus + mailComponent.Bounty;
                var expectedPenalty = teleporterComponent.PriorityMalus + mailComponent.Penalty;

                Assert.That(mailPriorityComponent.Bounty, Is.EqualTo(expectedBounty),
                    $"Priority mail did not have priority bonus on its bounty.");
                Assert.That(mailPriorityComponent.Penalty, Is.EqualTo(expectedPenalty),
                    $"Priority mail did not have priority malus on its penalty.");
            });

            await pairTracker.CleanReturnAsync();
        }


        [Test]
        public async Task TestFragileBonusMalus()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);
                EntityUid mailFragile = entityManager.SpawnEntity("TestMailFragileOnSpawn", coordinates);

                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "passenger", new HashSet<string>());
                mailSystem.SetupMail(mailFragile, teleporterComponent, "Bob", "passenger", new HashSet<string>());

                var mailComponent = entityManager.GetComponent<MailComponent>(mail);
                var mailFragileComponent = entityManager.GetComponent<MailComponent>(mailFragile);

                var expectedBounty = teleporterComponent.FragileBonus + mailComponent.Bounty;
                var expectedPenalty = teleporterComponent.FragileMalus + mailComponent.Penalty;

                Assert.That(mailFragileComponent.Bounty, Is.EqualTo(expectedBounty),
                    $"Fragile mail did not have fragile bonus on its bounty.");
                Assert.That(mailFragileComponent.Penalty, Is.EqualTo(expectedPenalty),
                    $"Fragile mail did not have fragile malus on its penalty.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestFragileDetection()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid mail = entityManager.SpawnEntity("TestMailFragileDetection", coordinates);

                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "passenger", new HashSet<string>());

                var mailComponent = entityManager.GetComponent<MailComponent>(mail);

                Assert.IsTrue(mailComponent.IsFragile,
                    $"Mail parcel with an empty drink glass inside was not detected as fragile.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailSystemMatchJobTitleToDepartment()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

#nullable enable
                mailSystem.TryMatchJobTitleToDepartment("passenger", out string? jobDepartment);
#nullable disable

                Assert.IsNotNull(jobDepartment,
                    "MailSystem was unable to match the passenger job title to a department.");

                Assert.That(jobDepartment, Is.EqualTo("Civilian"),
                    "MailSystem was unable to match the passenger job title to the Civilian department.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailSystemMatchJobTitleToIcon()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

#nullable enable
                mailSystem.TryMatchJobTitleToIcon("passenger", out string? jobIcon);
#nullable disable

                Assert.IsNotNull(jobIcon,
                    "MailSystem was unable to match the passenger job title to a job icon.");

                Assert.That(jobIcon, Is.EqualTo("Passenger"),
                    "MailSystem was unable to match the passenger job title to the Passenger job icon.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailJobStampVisuals()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var appearanceSystem = entitySystemManager.GetEntitySystem<SharedAppearanceSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);

                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "passenger", new HashSet<string>());

                var mailComponent = entityManager.GetComponent<MailComponent>(mail);

                Assert.IsTrue(appearanceSystem.TryGetData(mail, MailVisuals.JobIcon, out var jobIcon),
                    "Mail parcel was without MailVisuals.JobIcon appearance data for the job of Passenger.");

                Assert.IsInstanceOf<string>(jobIcon,
                    "MailVisuals.JobIcon was not a string.");

                Assert.That((string) jobIcon, Is.EqualTo("Passenger"),
                    $"The passenger job was not matched to the Passenger icon. Instead got: {(string) jobIcon}.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailTransferDamage()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var containerSystem = entitySystemManager.GetEntitySystem<SharedContainerSystem>();
            var damageableSystem = entitySystemManager.GetEntitySystem<DamageableSystem>();

            EntityUid? drinkGlass = null;

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid mail = entityManager.SpawnEntity("TestMailFragileDetection", coordinates);
                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "passenger", new HashSet<string>());

                Assert.IsTrue(containerSystem.TryGetContainer(mail, "contents", out var contents),
                    "Mail did not have contents container.");

                Assert.That(contents.ContainedEntities.Count, Is.EqualTo(1),
                    "TestMailFragileDetection's contents Count was not exactly 1.");

                drinkGlass = contents.ContainedEntities[0];

                var damageSpec = new DamageSpecifier(prototypeManager.Index<DamageTypePrototype>("Blunt"), 10);
                var damageResult = damageableSystem.TryChangeDamage(mail, damageSpec);

                Assert.Greater((int) damageResult.Total, 0,
                    "Mail transferred damage was not greater than 0.");
            });
            await server.WaitRunTicks(5);
            await server.WaitAssertion(() =>
            {
                Assert.IsTrue(entityManager.Deleted(drinkGlass),
                    $"DrinkGlass inside mail experiencing transferred damage was not marked as deleted.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailPriorityTimeoutPenalty()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

#nullable enable
            MailComponent? mailComponent = null;
#nullable disable

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporterAlwaysPriorityAlwaysBrutal", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);
                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "passenger", new HashSet<string>());

                mailComponent = entityManager.GetComponent<MailComponent>(mail);
            });
            await server.WaitRunTicks(100);
            await server.WaitAssertion(() =>
            {
                Assert.IsFalse(mailComponent.IsProfitable,
                    $"Priority mail was still IsProfitable after the Priority timeout period.");
            });

            await pairTracker.CleanReturnAsync();
        }

        private IMapGrid CreateDummyTileMapGrid(IMapManager mapManager, ITileDefinitionManager tileDefinitionManager)
        {
            var mapId = mapManager.CreateMap();

            mapManager.AddUninitializedMap(mapId);

            IMapGrid grid = mapManager.CreateGrid(mapId);

            var tileDefinition = tileDefinitionManager["UnderPlating"];
            var tile = new Tile(tileDefinition.TileId);
            var coordinates = grid.ToCoordinates();

            grid.SetTile(coordinates, tile);

            mapManager.DoMapInitialize(mapId);

            return grid;
        }

        [Test]
        public async Task TestMailTeleporterCanDetectMailOnItsTile()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            IMapGrid grid = default;

            await server.WaitPost(() =>
            {
                grid = CreateDummyTileMapGrid(mapManager, tileDefinitionManager);
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                var coordinates = grid.ToCoordinates();

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);

                var undeliveredParcelCount = mailSystem.GetUndeliveredParcelCount(teleporter);

                Assert.That(undeliveredParcelCount, Is.EqualTo(1),
                    "MailTeleporter isn't detecting undelivered parcels on its tile.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailTeleporterCanSpawnMail()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();

            IMapGrid grid = default;

            await server.WaitPost(() =>
            {
                var mapId = mapManager.CreateMap();

                mapManager.AddUninitializedMap(mapId);

                grid = mapManager.CreateGrid(mapId);

                var tileDefinition = tileDefinitionManager["UnderPlating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                mapManager.DoMapInitialize(mapId);
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                var coordinates = grid.ToCoordinates();

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid realCandidate1 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate1ID = entityManager.SpawnEntity("ClownIDCard", coordinates);

                idCardSystem.TryChangeFullName(realCandidate1ID, "Bob the Clown");
                idCardSystem.TryChangeJobTitle(realCandidate1ID, "Clown");

                Assert.IsTrue(handsSystem.TryPickup(realCandidate1, realCandidate1ID),
                    "Human dummy candidate could not pick up his ID.");

                mailSystem.SpawnMail(teleporter, teleporterComponent);

                var undeliveredParcelCount = mailSystem.GetUndeliveredParcelCount(teleporter);

                Assert.That(undeliveredParcelCount, Is.GreaterThan(0),
                    "MailTeleporter failed to teleport in mail.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailLimitUndeliveredParcels()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();
            var tileDefinitionManager = server.ResolveDependency<ITileDefinitionManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();

            IMapGrid grid = default;

            await server.WaitPost(() =>
            {
                var mapId = mapManager.CreateMap();

                mapManager.AddUninitializedMap(mapId);

                grid = mapManager.CreateGrid(mapId);

                var tileDefinition = tileDefinitionManager["UnderPlating"];
                var tile = new Tile(tileDefinition.TileId);
                var coordinates = grid.ToCoordinates();

                grid.SetTile(coordinates, tile);

                mapManager.DoMapInitialize(mapId);
            });

            await server.WaitRunTicks(5);

            await server.WaitAssertion(() =>
            {
                var coordinates = grid.ToCoordinates();

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporterAlwaysOneAtATime", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid realCandidate1 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate1ID = entityManager.SpawnEntity("ClownIDCard", coordinates);

                idCardSystem.TryChangeFullName(realCandidate1ID, "Bob the Clown");
                idCardSystem.TryChangeJobTitle(realCandidate1ID, "Clown");

                Assert.IsTrue(handsSystem.TryPickup(realCandidate1, realCandidate1ID),
                    "Human dummy candidate could not pick up his ID.");

                for (int i = 0; i < 6; ++i)
                    mailSystem.SpawnMail(teleporter, teleporterComponent);

                var undeliveredParcelCount = mailSystem.GetUndeliveredParcelCount(teleporter);

                Assert.That(undeliveredParcelCount, Is.EqualTo(1),
                    "MailTeleporter teleported in mail beyond its MaximumUndeliveredParcels.");
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}

