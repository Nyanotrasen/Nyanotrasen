using Robust.Shared.Timing;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// This system allows a generator to slowly decay over time.
/// </summary>
// TODO: PowerSuppliers which consume fuel instead.
public sealed class FinitePowerSupplierSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FinitePowerSupplierComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, FinitePowerSupplierComponent component, ComponentStartup args)
    {
        // Prevent reconstruction exploits. There's a better way to handle this,
        // but it would involve storing state on the machine circuitboard or its parts.
        // This will do for now.
        RemComp<ConstructionComponent>(uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<FinitePowerSupplierComponent, PowerSupplierComponent>();
        while (query.MoveNext(out var uid, out var component, out var powerSupplier))
        {
            if (curTime < component.NextDecayTick)
                continue;

            component.NextDecayTick = curTime + component.DecayInterval;

            powerSupplier.MaxSupply *= component.DecayRate;
        }
    }
}
