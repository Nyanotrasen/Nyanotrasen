#nullable enable
using NUnit.Framework;
using System.Threading.Tasks;
using Content.Shared.ReverseEngineering;
using Content.Shared.Research.Prototypes;
using Content.Server.ReverseEngineering;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ReverseEngineering
{
    [TestFixture]
    [TestOf(typeof(ReverseEngineeringSystem))]
    public sealed class ReverseEngineeringTest
    {
        [Test]
        public async Task ReverseEngineeringResultsValid()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;
            // Per RobustIntegrationTest.cs, wait until state is settled to access it.
            await server.WaitIdleAsync();

            var prototypeManager = server.ResolveDependency<IPrototypeManager>();

            await server.WaitAssertion(() =>
            {
                var allProtos = prototypeManager.EnumeratePrototypes<EntityPrototype>();

                foreach (var proto in allProtos)
                {
                    if (proto.Abstract || !proto.TryGetComponent<ReverseEngineeringComponent>(out var rev))
                        continue;

                    if (rev.Recipes != null)
                    {
                        foreach (var recipe in rev.Recipes)
                        {
                            Assert.IsTrue(prototypeManager.TryIndex<LatheRecipePrototype>(recipe, out var recipeProto),
                                "Could not index lathe recipe " + recipe + " for reverse engineering component on entity prototype " + proto.ID);

                            if (rev.NewItem == null && !rev.Generic)
                            {
                                Assert.IsTrue(recipeProto?.Result == proto.ID,
                                    "Reverse engineering " + proto.ID + " results in a different entity: " + recipeProto?.Result);
                            }
                        }
                    }

                    if (rev.NewItem != null)
                    {
                        Assert.IsTrue(prototypeManager.TryIndex<EntityPrototype>(rev.NewItem, out var entProto),
                            "Reverse engineering recipe on " + proto.ID + " tries to spawn invalid entity prototype " + rev.NewItem);

                        if (entProto?.ID != null)
                            Assert.IsTrue(entProto.ID != proto.ID, "Newitem field on " + proto.ID + " tries to spawn itself. Not what that field is for.");
                    }
                }
            });
        }
    }
}
