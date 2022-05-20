using Content.Server.Fluids.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;

/// yeah this could be a bit less duplicated from DrainSystem

namespace Content.Server.FloorBuffer
{
    public sealed class FloorBufferSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var buffer in EntityQuery<FloorBufferComponent>())
            {
                buffer.Accumulator += frameTime;
                if (buffer.Accumulator < buffer.BufferFrequency)
                {
                    continue;
                }
                buffer.Accumulator -= buffer.BufferFrequency;

                var amount = buffer.UnitsPerSecond * buffer.BufferFrequency;
                var xform = Transform(buffer.Owner);
                List<PuddleComponent> puddles = new();

                foreach (var entity in xform.Coordinates.GetEntitiesInTile())
                {
                    if (TryComp<PuddleComponent>(entity, out var puddleComp))
                        puddles.Add(puddleComp);
                }

                if (puddles.Count == 0)
                    continue;

                amount /= puddles.Count;

                foreach (var puddle in puddles)
                {
                    if (!_solutionSystem.TryGetSolution(puddle.Owner, puddle.SolutionName, out var puddleSolution))
                    {
                        EntityManager.QueueDeleteEntity(puddle.Owner);
                        continue;
                    }

                    _solutionSystem.SplitSolution(puddle.Owner, puddleSolution,
                        FixedPoint2.Min(FixedPoint2.New(amount), puddleSolution.CurrentVolume));

                    if (puddleSolution.CurrentVolume <= 0)
                    {
                        EntityManager.QueueDeleteEntity(puddle.Owner);
                    }
                }
            }
        }
    }
}
