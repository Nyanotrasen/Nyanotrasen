#nullable enable
using NUnit.Framework;
using System.Threading.Tasks;
using Content.Shared.Random;
using Content.Server.Chapel;
using Content.Server.Abilities.Psionics;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

/// <summary>
/// We use a lot of 'weighted random' prototypes that don't lint or give indication of what sort of prototype collection they have.
/// So, we'll make sure everything can be indexed here.
/// </summary>
namespace Content.IntegrationTests.Tests.Random
{
    [TestFixture]
    [TestOf(typeof(WeightedRandomPrototype))]
    [TestOf(typeof(SacrificialAltarSystem))]
    [TestOf(typeof(PsionicAbilitiesSystem))]
    public sealed class NyanoRandomTest
    {
        [Test]
        public async Task TestAltarRewards()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            // Per RobustIntegrationTest.cs, wait until state is settled to access it.
            await server.WaitIdleAsync();

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                Assert.IsTrue(prototypeManager.TryIndex<WeightedRandomPrototype>("PsionicArtifactPool", out var pool),
                    "Could not index PsionicArtifactPool");

                if (pool != null)
                {
                    foreach (var proto in pool.Weights.Keys)
                    {
                        Assert.IsTrue(prototypeManager.HasIndex<EntityPrototype>(proto),
                            $"{proto} is in PsionicArtifactPool but is not defined anywhere.");
                    }
                }
            });

            await pairTracker.CleanReturnAsync();
        }

        [Test]
        public async Task TestPsionicPowerPool()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            // Per RobustIntegrationTest.cs, wait until state is settled to access it.
            await server.WaitIdleAsync();

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var componentFactory = server.ResolveDependency<IComponentFactory>();

            await server.WaitAssertion(() =>
            {
                Assert.IsTrue(prototypeManager.TryIndex<WeightedRandomPrototype>("RandomPsionicPowerPool", out var pool),
                    "Could not index RandomPsionicPowerPool");

                if (pool != null)
                {
                    foreach (var power in pool.Weights.Keys)
                    {
                        Assert.DoesNotThrow(() => componentFactory.GetComponent(power),
                            $"{power} is in RandomPsionicPowerPool but is not a valid component.");
                    }
                }
            });

            await pairTracker.CleanReturnAsync();
        }
    }
}
