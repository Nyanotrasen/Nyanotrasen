using Content.Shared.Interaction;
using Content.Shared.ReverseEngineering;
using Content.Server.Research.TechnologyDisk.Components;
using Content.Server.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Timing;
using Robust.Server.GameObjects;

namespace Content.Server.ReverseEngineering;

public sealed class ReverseEngineeringSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const string TargetSlot = "target_slot";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReverseEngineeringMachineComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<ReverseEngineeringMachineComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        SubscribeLocalEvent<ReverseEngineeringMachineComponent, ReverseEngineeringMachineScanButtonPressedMessage>(OnScanButtonPressed);

        SubscribeLocalEvent<ReverseEngineeringMachineComponent, BeforeActivatableUIOpenEvent>((e,c,_) => UpdateUserInterface(e,c));
        // SubscribeLocalEvent<ReverseEngineeringMachineComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (active, rev) in EntityQuery<ActiveReverseEngineeringMachineComponent, ReverseEngineeringMachineComponent>())
        {
            UpdateUserInterface(rev.Owner, rev);

            if (_timing.CurTime - active.StartTime < rev.AnalysisDuration)
                continue;

            FinishProbe(rev.Owner, rev, active);
        }}

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

    private void OnScanButtonPressed(EntityUid uid, ReverseEngineeringMachineComponent component, ReverseEngineeringMachineScanButtonPressedMessage args)
    {
        if (component.CurrentItem == null)
            return;

        if (HasComp<ActiveReverseEngineeringMachineComponent>(uid))
            return;

        var activeComp = EnsureComp<ActiveReverseEngineeringMachineComponent>(uid);
        activeComp.StartTime = _timing.CurTime;
        activeComp.Item = component.CurrentItem.Value;
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
        var scanning = TryComp<ActiveReverseEngineeringMachineComponent>(uid, out var active);
        var canScan = (item != null && !scanning);
        var remaining = active != null ? _timing.CurTime - active.StartTime : TimeSpan.Zero;

        var state = new ReverseEngineeringMachineScanUpdateState(item, canScan, msg, scanning, remaining, component.AnalysisDuration);

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

    private void FinishProbe(EntityUid uid, ReverseEngineeringMachineComponent? component = null, ActiveReverseEngineeringMachineComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active))
            return;

        if (!TryComp<ReverseEngineeringComponent>(component.CurrentItem, out var rev))
        {
            Logger.Error("We somehow scanned a " + component.CurrentItem + " for reverse engineering...");
            return;
        }

        var disk = Spawn(component.DiskPrototype, Transform(uid).Coordinates);

        if (!TryComp<TechnologyDiskComponent>(disk, out var diskComponent))
            return;

        diskComponent.Recipes = rev.Recipes;
        component.CurrentItem = null;
        Del(rev.Owner); // todo: eject
        RemComp<ActiveReverseEngineeringMachineComponent>(uid);
        UpdateUserInterface(uid, component);
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
