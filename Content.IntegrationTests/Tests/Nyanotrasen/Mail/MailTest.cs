using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mail;
using Content.Server.Access.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Mail;
using Content.Server.Mail.Components;
using Content.Server.Station.Systems;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    [TestOf(typeof(MailSystem))]
    [TestOf(typeof(MailComponent))]
    [TestOf(typeof(MailTeleporterComponent))]
    public sealed class MailTest
    {
        private const string Prototypes = @"
- type: mailDeliveryPool
  id: MailPoolHonk
  jobs:
    Clown:
        TestMailHonk: 1

- type: mailDeliveryPool
  id: MailPoolHonkAndNothing
  jobs:
    Clown:
        TestMailHonk: 100
  departments:
    Civilian:
        TestMailBottleOfNothing: 0.01

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
  id: TestMailTeleporterAlwaysHonks
  parent: BaseStructureDynamic
  name: TestMailTeleporterAlwaysHonks
  components:
  - type: MailTeleporter
    mailPool: MailPoolHonk

- type: entity
  id: TestMailTeleporterHonkAndNothing
  parent: BaseStructureDynamic
  name: TestMailTeleporterHonkAndNothing
  components:
  - type: MailTeleporter
    mailPool: MailPoolHonkAndNothing

- type: entity
  parent: BaseMail
  id: TestMail
  name: TestMail

- type: entity
  parent: BaseMail
  id: TestMailHonk
  name: TestMailHonk
  components:
  - type: Mail
    contents:
    - id: BikeHorn

- type: entity
  parent: BaseMail
  id: TestMailBottleOfNothing
  name: TestMailBottleOfNothing
  components:
  - type: Mail
    contents:
    - id: DrinkBottleOfNothingFull

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
        public async Task TestAllMailIsAvailableToSpawn()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                // Collect all the mail that's somewhere in a pool.
                List<string> availableMail = new();

                foreach (var pool in prototypeManager.EnumeratePrototypes<MailDeliveryPoolPrototype>())
                {
                    availableMail.AddRange(pool.Everyone.Keys);

                    foreach (var job in pool.Jobs)
                        availableMail.AddRange(job.Value.Keys);

                    foreach (var department in pool.Departments)
                        availableMail.AddRange(department.Value.Keys);
                }

                // Check all mail that's defined and see if it exists in this pool of pools.
                foreach (var entityPrototype in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (entityPrototype.Parents == null || !entityPrototype.Parents.Contains("BaseMail"))
                        continue;

                    Assert.That(availableMail.Contains(entityPrototype.ID),
                        $"{entityPrototype.ID} is declared but was not found in any MailDeliveryPool.");
                }
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestAllMailHasSomeContents()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                foreach (var entityPrototype in prototypeManager.EnumeratePrototypes<EntityPrototype>())
                {
                    if (entityPrototype.Parents == null || !entityPrototype.Parents.Contains("BaseMail"))
                        continue;

                    var mailComponent = entityPrototype.Components["Mail"].Component;
                    if (mailComponent is MailComponent mailComponentCast)
                    {
                        Assert.That(mailComponentCast.Contents.Count, Is.GreaterThan(0),
                            $"{entityPrototype.ID} does not have any contents.");
                    }
                }
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestTeleporterCanSpawnPriorityMail()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporterAlwaysPriority", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);

                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "passenger", new HashSet<string>());

                var mailComponent = entityManager.GetComponent<MailComponent>(mail);

                Assert.IsTrue(mailComponent.IsPriority,
                    $"MailTeleporter failed to spawn priority mail when PriorityChance set to 1");
            });
            await server.WaitPost(() =>
            {
                // Because Server/Client pairs can be re-used between Tests, we
                // need to clean up anything that might affect other tests,
                // otherwise this pair cannot be considered clean, and the
                // CleanReturnAsync call would need to be removed.
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailPriorityBonusMalus()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

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
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailFragileBonusMalus()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

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
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailFragileDetection()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

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
                    $"The passenger job was not matched to the Passenger icon.");
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

        [Test]
        public async Task TestMailCanBeUnlockedWithValidID()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();

            EntityUid mail = default;
            MailComponent mailComponent = default;

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var coordinates = new MapCoordinates(Vector2.Zero, mapId);

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid realCandidate1 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate1ID = entityManager.SpawnEntity("ClownIDCard", coordinates);

                var name = "Bob";
                var job = "clown";

                idCardSystem.TryChangeFullName(realCandidate1ID, name);
                idCardSystem.TryChangeJobTitle(realCandidate1ID, job);

                mail = entityManager.SpawnEntity("TestMail", coordinates);
                mailComponent = entityManager.GetComponent<MailComponent>(mail);
                mailSystem.SetupMail(mail, teleporterComponent, name, job, new HashSet<string>());

                Assert.IsTrue(handsSystem.TryPickup(realCandidate1, realCandidate1ID),
                    "Human dummy candidate could not pick up his ID.");

                entityManager.EventBus.RaiseLocalEvent(mail,
                    new AfterInteractUsingEvent(
                        realCandidate1,
                        realCandidate1ID,
                        mail,
                        new EntityCoordinates(mail, 0, 0),
                        true)
                    );
            });
            await server.WaitRunTicks(5);
            await server.WaitAssertion(() =>
            {
                Assert.IsFalse(mailComponent.IsLocked,
                    "Mail is still IsLocked after being interacted with a valid ID.");
                Assert.IsFalse(mailComponent.IsProfitable,
                    "Mail is still IsProfitable after being unlocked.");
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailCannotBeUnlockedWithInvalidID()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            MailComponent mailComponent = default;

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid realCandidate1 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate1ID = entityManager.SpawnEntity("ClownIDCard", coordinates);

                idCardSystem.TryChangeFullName(realCandidate1ID, "Not Bob");
                idCardSystem.TryChangeJobTitle(realCandidate1ID, "Not Clown");

                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);
                mailComponent = entityManager.GetComponent<MailComponent>(mail);
                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "clown", new HashSet<string>());

                Assert.IsTrue(handsSystem.TryPickup(realCandidate1, realCandidate1ID),
                    "Human dummy candidate could not pick up his ID.");

                entityManager.EventBus.RaiseLocalEvent(mail,
                    new AfterInteractUsingEvent(
                        realCandidate1,
                        realCandidate1ID,
                        mail,
                        new EntityCoordinates(mail, 0, 0),
                        true)
                    );
            });
            await server.WaitRunTicks(5);
            await server.WaitAssertion(() =>
            {
                Assert.IsTrue(mailComponent.IsLocked,
                    "Mail is not IsLocked after being interacted with an invalid ID.");
                Assert.IsTrue(mailComponent.IsProfitable,
                    "Mail is not IsProfitable after being interacted with an invalid ID.");
            });
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailTeleporterCanDetectMailOnItsTile()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);

                var undeliveredParcelCount = mailSystem.GetUndeliveredParcelCount(teleporter);

                Assert.That(undeliveredParcelCount, Is.EqualTo(0),
                    "MailTeleporter somehow had mail on its tile at spawn.");

                EntityUid mail = entityManager.SpawnEntity("TestMail", coordinates);

                undeliveredParcelCount = mailSystem.GetUndeliveredParcelCount(teleporter);

                Assert.That(undeliveredParcelCount, Is.EqualTo(1),
                    "MailTeleporter isn't detecting undelivered parcels on its tile.");
            });
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailTeleporterCanSpawnMail()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

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
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailLimitUndeliveredParcels()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

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

                Assert.That(undeliveredParcelCount, Is.GreaterThan(0),
                    "MailTeleporter didn't teleport in any mail.");
                Assert.That(undeliveredParcelCount, Is.EqualTo(1),
                    "MailTeleporter teleported in mail beyond its MaximumUndeliveredParcels.");
            });
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailDepositsIntoStationBankAccount()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();
            var stationSystem = entitySystemManager.GetEntitySystem<StationSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            EntityUid mail = default;
            MailComponent mailComponent = default;
            AfterInteractUsingEvent eventArgs = default;
#nullable enable
            StationBankAccountComponent? stationBankAccountComponent = null;
            int? previousBalance = null;
#nullable disable

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;
                stationSystem.InitializeNewStation(null, new List<EntityUid>() {testMap.MapGrid.GridEntityId}, $"Clown Town");

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid realCandidate1 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate1ID = entityManager.SpawnEntity("ClownIDCard", coordinates);

                var name = "Bob";
                var job = "clown";

                idCardSystem.TryChangeFullName(realCandidate1ID, name);
                idCardSystem.TryChangeJobTitle(realCandidate1ID, job);

                mail = entityManager.SpawnEntity("TestMail", coordinates);
                mailComponent = entityManager.GetComponent<MailComponent>(mail);
                mailSystem.SetupMail(mail, teleporterComponent, name, job, new HashSet<string>());

                Assert.IsTrue(handsSystem.TryPickup(realCandidate1, realCandidate1ID),
                    "Human dummy candidate could not pick up his ID.");

                eventArgs = new AfterInteractUsingEvent(
                    realCandidate1,
                    realCandidate1ID,
                    mail,
                    new EntityCoordinates(mail, 0, 0),
                    true);
            });
            await server.WaitRunTicks(5);
            await server.WaitAssertion(() =>
            {
                foreach (var account in entityManager.EntityQuery<StationBankAccountComponent>())
                {
                    if (stationSystem.GetOwningStation(account.Owner) != stationSystem.GetOwningStation(mail))
                            continue;

                    stationBankAccountComponent = account;
                    previousBalance = account.Balance;
                    break;
                }

                Assert.IsNotNull(stationBankAccountComponent,
                    "Unable to find matching StationBankAccountComponent for mail parcel.");

                entityManager.EventBus.RaiseLocalEvent(mail, eventArgs);
            });
            await server.WaitRunTicks(5);
            await server.WaitAssertion(() =>
            {
                var currentBalance = stationBankAccountComponent.Balance;

                // This shouldn't happen:
                Assert.IsNotNull(previousBalance,
                    "previousBalance was never assigned.");

                Assert.That(currentBalance, Is.GreaterThan(previousBalance.Value),
                    "StationBankAccountComponent's balance did not increase.");
                Assert.That(currentBalance, Is.EqualTo(previousBalance.Value + mailComponent.Bounty),
                    "StationBankAccountComponent had incorrect balance.");
            });
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailPenalizesStationBankAccountOnFailure()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();
            var stationSystem = entitySystemManager.GetEntitySystem<StationSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            EntityUid mail = default;
            MailComponent mailComponent = default;
#nullable enable
            StationBankAccountComponent? stationBankAccountComponent = null;
            int? previousBalance = null;
#nullable disable

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;
                stationSystem.InitializeNewStation(null, new List<EntityUid>() {testMap.MapGrid.GridEntityId}, $"Clown Town");

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                mail = entityManager.SpawnEntity("TestMail", coordinates);
                mailComponent = entityManager.GetComponent<MailComponent>(mail);
                mailSystem.SetupMail(mail, teleporterComponent, "Bob", "clown", new HashSet<string>());

            });
            await server.WaitRunTicks(5);
            await server.WaitAssertion(() =>
            {
                foreach (var account in entityManager.EntityQuery<StationBankAccountComponent>())
                {
                    if (stationSystem.GetOwningStation(account.Owner) != stationSystem.GetOwningStation(mail))
                            continue;

                    stationBankAccountComponent = account;
                    previousBalance = account.Balance;
                    break;
                }

                Assert.IsNotNull(stationBankAccountComponent,
                    "Unable to find matching StationBankAccountComponent for mail parcel.");

                mailSystem.PenalizeStationFailedDelivery(mail, mailComponent, "mail-penalty-lock");
            });
            await server.WaitRunTicks(5);
            await server.WaitAssertion(() =>
            {
                var currentBalance = stationBankAccountComponent.Balance;

                // This shouldn't happen:
                Assert.IsNotNull(previousBalance,
                    "previousBalance was never assigned.");

                Assert.That(currentBalance, Is.LessThan(previousBalance.Value),
                    "StationBankAccountComponent's balance did not decrease.");
                Assert.That(currentBalance, Is.EqualTo(previousBalance.Value + mailComponent.Penalty),
                    "StationBankAccountComponent had incorrect balance.");
            });
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailSpawnForJobWithJobCandidate()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporterAlwaysHonks", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid realCandidate1 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate1ID = entityManager.SpawnEntity("ClownIDCard", coordinates);

                var name = "Bob";
                var job = "clown";

                idCardSystem.TryChangeFullName(realCandidate1ID, name);
                idCardSystem.TryChangeJobTitle(realCandidate1ID, job);

                Assert.IsTrue(handsSystem.TryPickup(realCandidate1, realCandidate1ID),
                    "Human dummy candidate could not pick up his ID.");

                mailSystem.SpawnMail(teleporter, teleporterComponent);

                var undeliveredParcelCount = mailSystem.GetUndeliveredParcelCount(teleporter);

                Assert.That(undeliveredParcelCount, Is.EqualTo(1),
                    "MailTeleporter failed to teleport in mail.");
            });
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailSpawnForJobWithoutJobCandidate()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();
            var containerSystem = entitySystemManager.GetEntitySystem<SharedContainerSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporterHonkAndNothing", coordinates);
                var teleporterComponent = entityManager.GetComponent<MailTeleporterComponent>(teleporter);

                EntityUid realCandidate1 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate1ID = entityManager.SpawnEntity("ClownIDCard", coordinates);

                var name = "Rob";
                var job = "mime";

                idCardSystem.TryChangeFullName(realCandidate1ID, name);
                idCardSystem.TryChangeJobTitle(realCandidate1ID, job);

                Assert.IsTrue(handsSystem.TryPickup(realCandidate1, realCandidate1ID),
                    "Human dummy candidate could not pick up his ID.");

                mailSystem.SpawnMail(teleporter, teleporterComponent);

                for (int i = 0; i < 5; ++i)
                    mailSystem.SpawnMail(teleporter, teleporterComponent);

                var undeliveredParcels = mailSystem.GetUndeliveredParcels(teleporter);

                Assert.That(undeliveredParcels.Count, Is.EqualTo(5),
                    "Failed to deliver the proper amount of mail for this test.");

                foreach (var mail in undeliveredParcels)
                {
                    // Statistical probabilty says that all of these should be
                    // bottles of nothing, if everything's working correctly.
                    Assert.IsTrue(containerSystem.TryGetContainer(mail, "contents", out var contents),
                        "Mail did not have contents container.");

                    Assert.That(contents.ContainedEntities.Count, Is.EqualTo(1),
                        "Mail's contents container did not have correct amount of entities.");

                    var entity = contents.ContainedEntities.First();
#nullable enable
                    Assert.IsTrue(entityManager.TryGetComponent(entity, out MetaDataComponent? metaDataComponent),
                        "Mail did not have MetaDataComponent.");
#nullable disable
                    Assert.That(metaDataComponent.EntityName, Is.EqualTo("bottle of nothing"),
                        "Mail did not contain bottle of nothing.");
                }
            });
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestMailRecipients()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
            var server = pairTracker.Pair.Server;

            var mapManager = server.ResolveDependency<IMapManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var entitySystemManager = server.ResolveDependency<IEntitySystemManager>();

            var mailSystem = entitySystemManager.GetEntitySystem<MailSystem>();
            var handsSystem = entitySystemManager.GetEntitySystem<SharedHandsSystem>();
            var idCardSystem = entitySystemManager.GetEntitySystem<IdCardSystem>();

            var testMap = await PoolManager.CreateTestMap(pairTracker);

            await server.WaitAssertion(() =>
            {
                var coordinates = testMap.GridCoords;

                EntityUid teleporter = entityManager.SpawnEntity("TestMailTeleporter", coordinates);

                // This might indicate entities that were not deleted between tests.
                Assert.That(mailSystem.GetMailRecipientCandidates(teleporter).Count, Is.EqualTo(0),
                    "Mail recipients for new mail teleporter was not empty.");

                EntityUid realCandidate1 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate1ID = entityManager.SpawnEntity("ClownIDCard", coordinates);

                idCardSystem.TryChangeFullName(realCandidate1ID, "Bob");
                idCardSystem.TryChangeJobTitle(realCandidate1ID, "clown");

                Assert.IsTrue(handsSystem.TryPickup(realCandidate1, realCandidate1ID),
                    "Human dummy candidate #1 could not pick up his ID.");

                Assert.That(mailSystem.GetMailRecipientCandidates(teleporter).Count, Is.EqualTo(1),
                    "Number of mail recipients is incorrect.");

                EntityUid realCandidate2 = entityManager.SpawnEntity("HumanDummy", coordinates);
                EntityUid realCandidate2ID = entityManager.SpawnEntity("MimeIDCard", coordinates);

                idCardSystem.TryChangeFullName(realCandidate2ID, "Rob");
                idCardSystem.TryChangeJobTitle(realCandidate2ID, "mime");

                Assert.IsTrue(handsSystem.TryPickup(realCandidate2, realCandidate2ID),
                    "Human dummy candidate #2 could not pick up his ID.");

                Assert.That(mailSystem.GetMailRecipientCandidates(teleporter).Count, Is.EqualTo(2),
                    "Number of mail recipients is incorrect.");
            });
            await server.WaitPost(() =>
            {
                mapManager.DeleteMap(testMap.MapId);
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
