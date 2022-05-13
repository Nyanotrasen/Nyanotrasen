﻿using Content.Server.Atmos.Components;
using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasAnalyzerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var analyzer in EntityManager.EntityQuery<GasAnalyzerComponent>(true))
            {
                analyzer.Update(frameTime);
            }
        }
    }
}
