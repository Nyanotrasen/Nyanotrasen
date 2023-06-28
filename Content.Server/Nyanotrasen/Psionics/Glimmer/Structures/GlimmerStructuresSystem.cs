using Content.Server.Power.EntitySystems;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.Psionics.Glimmer
{
    /// <summary>
    /// Handles structures which add/subtract glimmer.
    /// </summary>
    public sealed class GlimmerStructuresSystem : EntitySystem
    {
        [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var source in EntityQuery<GlimmerSourceComponent>())
            {
                if (!_powerReceiverSystem.IsPowered(source.Owner))
                    continue;

                if (!source.Active)
                    continue;

                source.Accumulator += frameTime;

                if (source.Accumulator > source.SecondsPerGlimmer)
                {
                    source.Accumulator -= source.SecondsPerGlimmer;
                    if (source.AddToGlimmer)
                    {
                        _glimmerSystem.Glimmer++;
                    }
                    else
                    {
                        _glimmerSystem.Glimmer--;
                    }
                }
            }
        }
    }
}
