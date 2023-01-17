using Content.Shared.Interaction;
using Content.Server.Research.TechnologyDisk.Components;
namespace Content.Server.ReverseEngineering;

public sealed class ReverseEngineeringSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReverseEngineeringMachineComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnAfterInteractUsing(EntityUid uid, ReverseEngineeringMachineComponent component, AfterInteractUsingEvent args)
    {
        if (!TryComp<ReverseEngineeringComponent>(args.Used, out var rev))
            return;

        var disk = Spawn(component.DiskPrototype, Transform(uid).Coordinates);

        if (!TryComp<TechnologyDiskComponent>(disk, out var diskComponent))
            return;

        diskComponent.Recipes = rev.Recipes;
    }
}
