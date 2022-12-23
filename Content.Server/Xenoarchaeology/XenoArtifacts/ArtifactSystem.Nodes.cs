using System.Linq;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared.Xenoarchaeology.XenoArtifacts;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Xenoarchaeology.XenoArtifacts;

public sealed partial class ArtifactSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private const int MaxEdgesPerNode = 4;

    /// <summary>
    /// Generate an Artifact tree with fully developed nodes.
    /// </summary>
    /// <param name="tree">The tree being generated.</param>
    /// <param name="nodeAmount">The amount of nodes it has.</param>
    private void GenerateArtifactNodeTree(EntityUid artifact, ref ArtifactTree tree, int nodeAmount)
    {
        if (nodeAmount < 1)
        {
            Logger.Error($"nodeAmount {nodeAmount} is less than 1. Aborting artifact tree generation.");
            return;
        }

        var uninitializedNodes = new List<ArtifactNode> { new() };
        tree.StartNode = uninitializedNodes.First(); //the first node

        while (uninitializedNodes.Any())
        {
            GenerateNode(artifact, ref uninitializedNodes, ref tree, nodeAmount);
        }
    }

    /// <summary>
    /// Generate an individual node on the tree.
    /// </summary>
    private void GenerateNode(EntityUid artifact, ref List<ArtifactNode> uninitializedNodes, ref ArtifactTree tree, int targetNodeAmount)
    {
        if (!uninitializedNodes.Any())
            return;

        var node = uninitializedNodes.First();
        uninitializedNodes.Remove(node);

        //random 5-digit number
        node.Id = _random.Next(10000, 100000);

        //Generate the connected nodes
        var maxEdges = Math.Max(1, targetNodeAmount - tree.AllNodes.Count - uninitializedNodes.Count - 1);
        maxEdges = Math.Min(maxEdges, MaxEdgesPerNode);
        var minEdges = Math.Clamp(targetNodeAmount - tree.AllNodes.Count - uninitializedNodes.Count - 1, 0, 1);

        var edgeAmount = _random.Next(minEdges, maxEdges);

        for (var i = 0; i < edgeAmount; i++)
        {
            var neighbor = new ArtifactNode
            {
                Depth = node.Depth + 1
            };
            node.Edges.Add(neighbor);
            neighbor.Edges.Add(node);

            uninitializedNodes.Add(neighbor);
        }

        node.Trigger = GetRandomTrigger(artifact, ref node);
        node.Effect = GetRandomEffect(artifact, ref node);

        tree.AllNodes.Add(node);
    }

    //yeah these two functions are near duplicates but i don't
    //want to implement an interface or abstract parent

    private ArtifactTriggerPrototype GetRandomTrigger(EntityUid artifact, ref ArtifactNode node)
    {
        var allTriggers = _prototype.EnumeratePrototypes<ArtifactTriggerPrototype>().ToList();
        var validDepth = allTriggers.Select(x => x.TargetDepth).Distinct().ToList();

        var weights = GetDepthWeights(validDepth, node.Depth);
        var selectedRandomTargetDepth = GetRandomTargetDepth(weights);
        var targetTriggers = allTriggers
            .Where(x => x.TargetDepth == selectedRandomTargetDepth)
            .Where(x => (x.Whitelist?.IsValid(artifact, EntityManager) ?? true) && (!x.Blacklist?.IsValid(artifact, EntityManager) ?? true)).ToList();


        return _random.Pick(targetTriggers);
    }

    private ArtifactEffectPrototype GetRandomEffect(EntityUid artifact, ref ArtifactNode node)
    {
        var allEffects = _prototype.EnumeratePrototypes<ArtifactEffectPrototype>().ToList();
        var validDepth = allEffects.Select(x => x.TargetDepth).Distinct().ToList();

        var weights = GetDepthWeights(validDepth, node.Depth);
        var selectedRandomTargetDepth = GetRandomTargetDepth(weights);
        var targetEffects = allEffects
            .Where(x => x.TargetDepth == selectedRandomTargetDepth)
            .Where(x => (x.Whitelist?.IsValid(artifact, EntityManager) ?? true) && (!x.Blacklist?.IsValid(artifact, EntityManager) ?? true)).ToList();

        return _random.Pick(targetEffects);
    }

    /// <remarks>
    /// The goal is that the depth that is closest to targetDepth has the highest chance of appearing.
    /// The issue is that we also want some variance, so levels that are +/- 1 should also have a
    /// decent shot of appearing. This function should probably get some tweaking at some point.
    /// </remarks>
    private Dictionary<int, float> GetDepthWeights(IEnumerable<int> depths, int targetDepth)
    {
        var weights = new Dictionary<int, float>();
        foreach (var d in depths)
        {
            //TODO: is this equation sus? idk. -emo
            // 0.3 / (|current_iterated_depth - our_actual_depth| + 1)^2
            var w = 0.3f / MathF.Pow(Math.Abs(d - targetDepth) + 1, 2);
            weights.Add(d, w);
        }
        return weights;
    }

    /// <summary>
    /// Uses a weighted random system to get a random depth.
    /// </summary>
    private int GetRandomTargetDepth(Dictionary<int, float> weights)
    {
        var sum = weights.Values.Sum();
        var accumulated = 0f;

        var rand = _random.NextFloat() * sum;

        foreach (var (key, weight) in weights)
        {
            accumulated += weight;

            if (accumulated >= rand)
            {
                return key;
            }
        }
        return _random.Pick(weights.Keys); //shouldn't happen
    }

    /// <summary>
    /// Enter a node: attach the relevant components
    /// </summary>
    private void EnterNode(EntityUid uid, ref ArtifactNode node, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.CurrentNode != null)
        {
            ExitNode(uid, component);
        }

        component.CurrentNode = node;

        var allComponents = node.Effect.Components.Concat(node.Effect.PermanentComponents).Concat(node.Trigger.Components);
        foreach (var (name, entry) in allComponents)
        {
            var reg = _componentFactory.GetRegistration(name);

            if (node.Discovered && EntityManager.HasComponent(uid, reg.Type))
            {
                // Don't re-add permanent components unless this is the first time you've entered this node
                if (node.Effect.PermanentComponents.ContainsKey(name))
                    continue;

                EntityManager.RemoveComponent(uid, reg.Type);
            }

            var comp = (Component) _componentFactory.GetComponent(reg);
            comp.Owner = uid;

            var temp = (object) comp;
            _serialization.CopyTo(entry.Component, ref temp);

            EntityManager.AddComponent(uid, (Component) temp!, true);
        }

        node.Discovered = true;
        RaiseLocalEvent(uid, new ArtifactNodeEnteredEvent(component.CurrentNode.Id));
    }

    /// <summary>
    /// Exit a node: remove the relevant components.
    /// </summary>
    private void ExitNode(EntityUid uid, ArtifactComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var node = component.CurrentNode;
        if (node == null)
            return;

        foreach (var name in node.Effect.Components.Keys.Concat(node.Trigger.Components.Keys))
        {
            var comp = _componentFactory.GetRegistration(name);
            EntityManager.RemoveComponentDeferred(uid, comp.Type);
        }

        component.CurrentNode = null;
    }
}
