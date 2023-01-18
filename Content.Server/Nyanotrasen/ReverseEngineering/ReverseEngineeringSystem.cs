using Content.Shared.Interaction;
using Content.Shared.ReverseEngineering;
using Content.Server.Research.TechnologyDisk.Components;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;

namespace Content.Server.ReverseEngineering;

public sealed class ReverseEngineeringSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const string TargetSlot = "target_slot";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReverseEngineeringMachineComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ReverseEngineeringMachineComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        // SubscribeLocalEvent<ReverseEngineeringMachineComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnEntInserted(EntityUid uid, ReverseEngineeringMachineComponent component, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != TargetSlot || !TryComp<ReverseEngineeringComponent>(args.Entity, out var rev))
            return;

        component.CurrentItem = args.Entity;
        component.CurrentItemDifficulty = rev.Difficulty;
        UpdateUserInterface(uid, component);
    }

    private void OnEntRemoved(EntityUid uid, ReverseEngineeringMachineComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != TargetSlot)
            return;

        component.CurrentItem = null;
        component.CurrentItemDifficulty = 0;
        UpdateUserInterface(uid, component);
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


    private void UpdateUserInterface(EntityUid uid, ReverseEngineeringMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        EntityUid? item = component.CurrentItem;
        FormattedMessage? msg = GetReverseEngineeringScanMessage(component);
        var totalTime = TimeSpan.Zero;
        var canScan = false;

        var state = new ReverseEngineeringMachineScanUpdateState(item, true, true, msg, false, TimeSpan.Zero, TimeSpan.FromMinutes(5));

        var bui = _ui.GetUi(uid, ReverseEngineeringMachineUiKey.Key);
        _ui.SetUiState(bui, state);
    }
    private ReverseEngineeringTickResult Roll(ReverseEngineeringMachineComponent component)
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

    private FormattedMessage? GetReverseEngineeringScanMessage(ReverseEngineeringMachineComponent component)
    {
        var msg = new FormattedMessage();

        if (component.CurrentItem == null)
        {
            msg.AddMarkup(Loc.GetString("reverse-engineering-status-ready"));
            return msg;
        }

        msg.AddMarkup(Loc.GetString("reverse-engineering-current-item", ("item", component.CurrentItem.Value)));
        msg.PushNewline();
        msg.PushNewline();

        msg.AddMarkup(Loc.GetString("reverse-engineering-item-difficulty", ("difficulty", component.CurrentItemDifficulty)));
        msg.PushNewline();
        msg.AddMarkup(Loc.GetString("reverse-engineering-progress", ("progress", component.Progress)));
        msg.PushNewline();

        string lastProbe = string.Empty;

        switch (component.LastResult)
        {
            case ReverseEngineeringTickResult.Destruction:
                lastProbe = Loc.GetString("reverse-engineering-failure");
                break;
            case ReverseEngineeringTickResult.Stagnation:
                lastProbe = Loc.GetString("reverse-engineering-stagnation");
                break;
            case ReverseEngineeringTickResult.SuccessMinor:
                lastProbe = Loc.GetString("reverse-engineering-minor");
                break;
            case ReverseEngineeringTickResult.SuccessAverage:
                lastProbe = Loc.GetString("reverse-engineering-average");
                break;
            case ReverseEngineeringTickResult.SuccessMajor:
                lastProbe = Loc.GetString("reverse-engineering-major");
                break;
            case ReverseEngineeringTickResult.InstantSuccess:
                lastProbe = Loc.GetString("reverse-engineering-success");
                break;
        }

        msg.AddMarkup(Loc.GetString("reverse-engineering-last-attempt-result", ("result", lastProbe)));

        return msg;
    }

}
