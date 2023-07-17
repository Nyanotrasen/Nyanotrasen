using System.Collections.Generic;
using System.Linq;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Content.Shared.ReverseEngineering;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class LatheRecipeAccessibility
{
    [Test]
    public async Task AllLatheRecipesAccessible()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings { NoClient = true });
        var server = pairTracker.Pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var allEnts = protoManager.EnumeratePrototypes<EntityPrototype>();
            var allLathes = new HashSet<LatheComponent>();
            var allReverseEngineerables = new HashSet<ReverseEngineeringComponent>();
            foreach (var proto in allEnts)
            {
                if (proto.Abstract)
                    continue;

                if (proto.TryGetComponent<LatheComponent>(out var lathe))
                    allLathes.Add(lathe);

                if (proto.TryGetComponent<ReverseEngineeringComponent>(out var reverseEngineering))
                    allReverseEngineerables.Add(reverseEngineering);
            }

            var latheStaticPrintables = new HashSet<string>();
            foreach (var lathe in allLathes)
            {
                latheStaticPrintables.UnionWith(lathe.StaticRecipes);
            }

            var reversables = new HashSet<string>();
            foreach (var reverseEngineerable in allReverseEngineerables)
            {
                if (reverseEngineerable.Recipes != null)
                    reversables.UnionWith(reverseEngineerable.Recipes);
            }

            var availablePathways = latheStaticPrintables.Union(reversables);

            Assert.Multiple(() =>
            {
                foreach (var recipe in protoManager.EnumeratePrototypes<LatheRecipePrototype>())
                {
                    if (availablePathways.Contains(recipe.ID))
                        continue;

                    var isResearchable = false;
                    foreach (var tech in protoManager.EnumeratePrototypes<TechnologyPrototype>())
                    {
                        if (tech.RecipeUnlocks.Contains(recipe.ID))
                        {
                            isResearchable = true;
                            break;
                        }
                    }

                    Assert.That(isResearchable, Is.True, $"Recipe \"{recipe.ID}\" cannot be researched, reverse-engineered, or printed as a static recipe.");
                }
            });
        });

        await pairTracker.CleanReturnAsync();
    }
}
