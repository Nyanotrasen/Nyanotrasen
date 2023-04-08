using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.UserInterface;
using Content.Server.Shuttles.Systems;
using Content.Server.Stack;
using Content.Server.Mind.Components;
using Content.Shared.Stacks;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.Whitelist;
using Content.Shared.Coordinates;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;


namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    /*
     * Handles cargo shuttle mechanics, including cargo shuttle consoles.
     */

    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    public MapId? CargoMap { get; private set; }

    private int _index;

    /// <summary>
    /// Whether cargo shuttles are enabled at all. Mainly used to disable cargo shuttle loading for performance reasons locally.
    /// </summary>
    private bool _enabled;

    private void InitializeShuttle()
    {
        _enabled = _configManager.GetCVar(CCVars.CargoShuttles);
        // Don't want to immediately call this as shuttles will get setup in the natural course of things.
        _configManager.OnValueChanged(CCVars.CargoShuttles, SetCargoShuttleEnabled);

        SubscribeLocalEvent<CargoShuttleComponent, FTLStartedEvent>(OnCargoFTLStarted);
        SubscribeLocalEvent<CargoShuttleComponent, FTLCompletedEvent>(OnCargoFTLCompleted);
        SubscribeLocalEvent<CargoShuttleComponent, FTLTagEvent>(OnCargoFTLTag);

        SubscribeLocalEvent<CargoShuttleConsoleComponent, ComponentStartup>(OnCargoShuttleConsoleStartup);

        SubscribeLocalEvent<CargoPalletConsoleComponent, CargoPalletSellMessage>(OnPalletSale);
        SubscribeLocalEvent<CargoPalletConsoleComponent, CargoPalletAppraiseMessage>(OnPalletAppraise);
        SubscribeLocalEvent<CargoPalletConsoleComponent, BoundUIOpenedEvent>(OnPalletUIOpen);

        SubscribeLocalEvent<CargoPilotConsoleComponent, ConsoleShuttleEvent>(OnCargoGetConsole);
        SubscribeLocalEvent<CargoPilotConsoleComponent, AfterActivatableUIOpenEvent>(OnCargoPilotConsoleOpen);
        SubscribeLocalEvent<CargoPilotConsoleComponent, BoundUIClosedEvent>(OnCargoPilotConsoleClose);

        SubscribeLocalEvent<StationCargoOrderDatabaseComponent, ComponentStartup>(OnCargoOrderStartup);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnCargoFTLTag(EntityUid uid, CargoShuttleComponent component, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        // Just saves mappers forgetting.
        args.Handled = true;
        args.Tag = "DockCargo";
    }

    private void ShutdownShuttle()
    {
        _configManager.UnsubValueChanged(CCVars.CargoShuttles, SetCargoShuttleEnabled);
    }

    private void SetCargoShuttleEnabled(bool value)
    {
        if (_enabled == value)
            return;

        _enabled = value;

        if (value)
        {
            Setup();

            foreach (var station in EntityQuery<StationCargoOrderDatabaseComponent>(true))
            {
                AddShuttle(station);
            }
        }
        else
        {
            CleanupShuttle();
        }
    }

    #region Cargo Pilot Console

    private void OnCargoPilotConsoleOpen(EntityUid uid, CargoPilotConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        component.Entity = GetShuttleConsole(uid);
    }

    private void OnCargoPilotConsoleClose(EntityUid uid, CargoPilotConsoleComponent component, BoundUIClosedEvent args)
    {
        component.Entity = null;
    }

    private void OnCargoGetConsole(EntityUid uid, CargoPilotConsoleComponent component, ref ConsoleShuttleEvent args)
    {
        args.Console = GetShuttleConsole(uid);
    }

    private EntityUid? GetShuttleConsole(EntityUid uid)
    {
        var stationUid = _station.GetOwningStation(uid);

        if (!TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase) ||
            !TryComp<CargoShuttleComponent>(orderDatabase.Shuttle, out var shuttle))
        {
            return null;
        }

        return GetShuttleConsole(orderDatabase.Shuttle.Value, shuttle);
    }

    #endregion

    #region Console

    private void UpdateCargoShuttleConsoles(CargoShuttleComponent component)
    {
        // Update pilot consoles that are already open.
        var pilotConsoleQuery = AllEntityQuery<CargoPilotConsoleComponent>();

        while (pilotConsoleQuery.MoveNext(out var uid, out var console))
        {
            var stationUid = _station.GetOwningStation(uid);
            if (stationUid == null || stationUid != component.Station)
                continue;

            console.Entity = GetShuttleConsole(stationUid.Value);
        }

        // Update order consoles.
        var shuttleConsoleQuery = AllEntityQuery<CargoShuttleConsoleComponent>();

        while (shuttleConsoleQuery.MoveNext(out var uid, out var console))
        {
            var stationUid = _station.GetOwningStation(uid);
            if (stationUid != component.Station)
                continue;

            UpdateShuttleState(uid, console, stationUid);
        }
    }

    private void UpdatePalletConsoleInterface(EntityUid uid, CargoPalletConsoleComponent component)
    {
        var bui = _uiSystem.GetUi(component.Owner, CargoPalletConsoleUiKey.Sale);
        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(bui,
            new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }
        GetPalletGoods(gridUid, out var toSell, out var amount, false);
        _uiSystem.SetUiState(bui,
            new CargoPalletConsoleInterfaceState((int) amount, toSell.Count, true));
    }

    private void OnPalletUIOpen(EntityUid uid, CargoPalletConsoleComponent component, BoundUIOpenedEvent args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        UpdatePalletConsoleInterface(uid, component);
    }

    /// <summary>
    /// Ok so this is just the same thing as opening the UI, its a refresh button.
    /// I know this would probably feel better if it were like predicted and dynamic as pallet contents change
    /// However.
    /// I dont want it to explode if cargo uses a conveyor to move 8000 pineapple slices or whatever, they are
    /// known for their entity spam i wouldnt put it past them
    /// </summary>

    private void OnPalletAppraise(EntityUid uid, CargoPalletConsoleComponent component, CargoPalletAppraiseMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        UpdatePalletConsoleInterface(uid, component);
    }

    private void OnCargoShuttleConsoleStartup(EntityUid uid, CargoShuttleConsoleComponent component, ComponentStartup args)
    {
        var station = _station.GetOwningStation(uid);
        UpdateShuttleState(uid, component, station);
    }

    private void UpdateShuttleState(EntityUid uid, CargoShuttleConsoleComponent component, EntityUid? station = null)
    {
        TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase);
        TryComp<CargoShuttleComponent>(orderDatabase?.Shuttle, out var shuttle);

        var orders = GetProjectedOrders(orderDatabase, shuttle);
        var shuttleName = orderDatabase?.Shuttle != null ? MetaData(orderDatabase.Shuttle.Value).EntityName : string.Empty;

        _uiSystem.GetUiOrNull(uid, CargoConsoleUiKey.Shuttle)?.SetState(
            new CargoShuttleConsoleBoundUserInterfaceState(
                station != null ? MetaData(station.Value).EntityName : Loc.GetString("cargo-shuttle-console-station-unknown"),
                string.IsNullOrEmpty(shuttleName) ? Loc.GetString("cargo-shuttle-console-shuttle-not-found") : shuttleName,
                orders));
    }

    #endregion

    #region Shuttle

    public EntityUid? GetShuttleConsole(EntityUid uid, CargoShuttleComponent component)
    {
        var query = AllEntityQuery<ShuttleConsoleComponent, TransformComponent>();

        while (query.MoveNext(out var cUid, out var comp, out var xform))
        {
            if (xform.ParentUid != uid)
                continue;

            return cUid;
        }

        return null;
    }

    /// <summary>
    /// Returns the orders that can fit on the cargo shuttle.
    /// </summary>
    private List<CargoOrderData> GetProjectedOrders(
        StationCargoOrderDatabaseComponent? component = null,
        CargoShuttleComponent? shuttle = null)
    {
        var orders = new List<CargoOrderData>();

        if (component == null || shuttle == null || component.Orders.Count == 0)
            return orders;

        var spaceRemaining = GetCargoSpace(shuttle.Owner);
        for( var i = 0; i < component.Orders.Count && spaceRemaining > 0; i++)
        {
            var order = component.Orders[i];
            if(order.Approved)
            {
                var numToShip = order.OrderQuantity - order.NumDispatched;
                if (numToShip > spaceRemaining)
                {
                    // We won't be able to fit the whole oder on, so make one
                    // which represents the space we do have left:
                    var reducedOrder = new CargoOrderData(order.OrderId,
                            order.ProductId, spaceRemaining, order.Requester, order.Reason);
                    orders.Add(reducedOrder);
                }
                else
                {
                    orders.Add(order);
                }
                spaceRemaining -= numToShip;
            }
        }

        return orders;
    }

    /// <summary>
    /// Get the amount of space the cargo shuttle can fit for orders.
    /// </summary>
    private int GetCargoSpace(EntityUid gridUid)
    {
        var space = GetCargoPallets(gridUid).Count;
        return space;
    }

    private List<CargoPalletComponent> GetCargoPallets(EntityUid gridUid)
    {
        var pads = new List<CargoPalletComponent>();

        foreach (var (comp, compXform) in EntityQuery<CargoPalletComponent, TransformComponent>(true))
        {
            if (compXform.ParentUid != gridUid ||
                !compXform.Anchored) continue;

            pads.Add(comp);
        }

        return pads;
    }

    #endregion

    #region Station

    private void OnCargoOrderStartup(EntityUid uid, StationCargoOrderDatabaseComponent component, ComponentStartup args)
    {
        if (!_enabled)
            return;

        // Stations get created first but if any are added at runtime then do this.
        AddShuttle(component);
    }

    private void AddShuttle(StationCargoOrderDatabaseComponent component)
    {
        Setup();

        if (CargoMap == null ||
            component.Shuttle != null ||
            component.CargoShuttleProto == null)
        {
            return;
        }

        var prototype = _protoMan.Index<CargoShuttlePrototype>(component.CargoShuttleProto);
        var possibleNames = _protoMan.Index<DatasetPrototype>(prototype.NameDataset).Values;
        var name = _random.Pick(possibleNames);

        if (!_map.TryLoad(CargoMap.Value, prototype.Path.ToString(), out var gridList))
        {
            _sawmill.Error($"Could not load the cargo shuttle!");
            return;
        }
        var shuttleUid = gridList[0];
        var xform = Transform(shuttleUid);
        MetaData(shuttleUid).EntityName = name;

        // TODO: Something better like a bounds check.
        xform.LocalPosition += 100 * _index;
        var comp = EnsureComp<CargoShuttleComponent>(shuttleUid);
        comp.Station = component.Owner;

        component.Shuttle = shuttleUid;
        UpdateCargoShuttleConsoles(comp);
        _index++;
        _sawmill.Info($"Added cargo shuttle to {ToPrettyString(shuttleUid)}");
    }

    private void SellPallets(EntityUid gridUid, out double amount)
    {
        GetPalletGoods(gridUid, out var toSell, out amount, true);

        _sawmill.Debug($"Cargo sold {toSell.Count} entities for {amount}");

        foreach (var ent in toSell)
        {
            Del(ent);
        }
    }

    private void GetPalletGoods(EntityUid gridUid, out HashSet<EntityUid> toSell, out double amount, bool sale = false)
    {
        amount = 0;
        var xformQuery = GetEntityQuery<TransformComponent>();
        var blacklistQuery = GetEntityQuery<CargoSellBlacklistComponent>();
        var mindQuery = GetEntityQuery<MindComponent>(); // for pallets especially, we don't want to risk instagibbing any players.

        toSell = new HashSet<EntityUid>();
        foreach (var pallet in GetCargoPallets(gridUid))
        {
            // Containers should already get the sell price of their children so can skip those.
            foreach (var ent in _lookup.GetEntitiesIntersecting(pallet.Owner, LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate))
            {
                // Dont sell:
                // - anything already being sold
                // - anything anchored (e.g. light fixtures)
                // - anything blacklisted (e.g. players).
                if (toSell.Contains(ent) ||
                    (xformQuery.TryGetComponent(ent, out var xform) && xform.Anchored))
                    continue;

                if (blacklistQuery.HasComponent(ent))
                    continue;

                if (mindQuery.HasComponent(ent))
                    continue;

                var price = _pricing.GetPrice(ent, sale);
                if (price == 0)
                    continue;
                toSell.Add(ent);
                amount += price;
            }
        }
    }

    private void AddCargoContents(CargoShuttleComponent shuttle, StationCargoOrderDatabaseComponent orderDatabase)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        var pads = GetCargoPallets(shuttle.Owner);
        while (pads.Count > 0)
        {
            var coordinates = new EntityCoordinates(shuttle.Owner, xformQuery.GetComponent(_random.PickAndTake(pads).Owner).LocalPosition);
            if(!FulfillOrder(orderDatabase, coordinates, shuttle.PrinterOutput))
            {
                break;
            }
        }
    }

    private void OnPalletSale(EntityUid uid, CargoPalletConsoleComponent component, CargoPalletSellMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        var bui = _uiSystem.GetUi(component.Owner, CargoPalletConsoleUiKey.Sale);
        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(bui,
            new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        var station = _station.GetOwningStation(uid);
        if (station == null)
            return;

        SellPallets(gridUid, out var price);
        foreach (var account in EntityQuery<StationBankAccountComponent>())
        {
            if (_station.GetOwningStation(account.Owner) != _station.GetOwningStation(uid))
                    continue;

            UpdateBankAccount(account, (int) price);
            return;
        }

        UpdatePalletConsoleInterface(uid, component);
    }

    private void OnCargoFTLStarted(EntityUid uid, CargoShuttleComponent component, ref FTLStartedEvent args)
    {
        var stationUid = component.Station;

        // Called
        if (CargoMap == null ||
            args.FromMapUid != _mapManager.GetMapEntityId(CargoMap.Value) ||
            !TryComp<StationCargoOrderDatabaseComponent>(stationUid, out var orderDatabase))
        {
            return;
        }

        AddCargoContents(component, orderDatabase);
        UpdateOrders(orderDatabase);
        UpdateCargoShuttleConsoles(component);
    }

    private void OnCargoFTLCompleted(EntityUid uid, CargoShuttleComponent component, ref FTLCompletedEvent args)
    {
        var xform = Transform(uid);
        // Recalled
        if (xform.MapID != CargoMap)
            return;

        var stationUid = component.Station;

        if (TryComp<StationBankAccountComponent>(stationUid, out var bank))
        {
            SellPallets(uid, out var amount);
            bank.Balance += (int) amount;
        }
    }

    #endregion

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanupShuttle();
    }

    private void CleanupShuttle()
    {
        if (CargoMap == null || !_mapManager.MapExists(CargoMap.Value))
        {
            CargoMap = null;
            DebugTools.Assert(!EntityQuery<CargoShuttleComponent>().Any());
            return;
        }

        _mapManager.DeleteMap(CargoMap.Value);
        CargoMap = null;

        // Shuttle may not have been in the cargo dimension (e.g. on the station map) so need to delete.
        var query = AllEntityQuery<CargoShuttleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (TryComp<StationCargoOrderDatabaseComponent>(comp.Station, out var station))
            {
                station.Shuttle = null;
            }
            QueueDel(uid);
        }
    }

    private void Setup()
    {
        if (!_enabled || CargoMap != null && _mapManager.MapExists(CargoMap.Value))
        {
            return;
        }

        // It gets mapinit which is okay... buuutt we still want it paused to avoid power draining.
        CargoMap = _mapManager.CreateMap();
        var mapUid = _mapManager.GetMapEntityId(CargoMap.Value);
        var ftl = EnsureComp<FTLDestinationComponent>(_mapManager.GetMapEntityId(CargoMap.Value));
        ftl.Whitelist = new EntityWhitelist()
        {
            Components = new[]
            {
                _factory.GetComponentName(typeof(CargoShuttleComponent))
            }
        };

        MetaData(mapUid).EntityName = $"Trading post {_random.Next(1000):000}";

        foreach (var comp in EntityQuery<StationCargoOrderDatabaseComponent>(true))
        {
            AddShuttle(comp);
        }

        _console.RefreshShuttleConsoles();
    }
}
