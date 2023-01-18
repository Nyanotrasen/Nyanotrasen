using Content.Shared.Interaction;
using Content.Shared.ReverseEngineering;
using Content.Server.Research.TechnologyDisk.Components;
using Robust.Shared.Random;

namespace Content.Server.ReverseEngineering;

public sealed class ReverseEngineeringSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();
        // SubscribeLocalEvent<ReverseEngineeringMachineComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnAfterInteractUsing(EntityUid uid, ReverseEngineeringMachineComponent component, AfterInteractUsingEvent args)
    {
        // Logger.Error("Roll result: " + Roll(component));
        // if (!TryComp<ReverseEngineeringComponent>(args.Used, out var rev))
        //     return;

        // var disk = Spawn(component.DiskPrototype, Transform(uid).Coordinates);

        // if (!TryComp<TechnologyDiskComponent>(disk, out var diskComponent))
        //     return;

        // diskComponent.Recipes = rev.Recipes;
    }

    public ReverseEngineeringTickResult Roll(ReverseEngineeringMachineComponent component)
    {
        int roll = (_random.Next(1, 6) + _random.Next(1, 6) + _random.Next(1, 6));

        roll += component.ScanBonus;
        roll += component.DangerBonus;
        roll -= component.CurrentItemDifficulty;

        return roll switch
        {
            <= 8 => ReverseEngineeringTickResult.Destruction,
            <= 10 => ReverseEngineeringTickResult.Stagnation,
            <= 12 => ReverseEngineeringTickResult.SuccessMinor,
            <= 15 => ReverseEngineeringTickResult.SuccessAverage,
            <= 17 => ReverseEngineeringTickResult.SuccessMajor,
            _ => ReverseEngineeringTickResult.InstantSuccess
        };
    }
}
