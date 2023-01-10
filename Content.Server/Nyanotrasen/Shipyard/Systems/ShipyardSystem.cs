using Content.Server.Shuttles.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Shipyard;
using Content.Server.Shipyard.Components;
using Content.Shared.MobState.Components;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Shipyard.Systems
{

    public sealed partial class ShipyardSystem : SharedShipyardSystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ShuttleSystem _shuttle = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly CargoSystem _cargo = default!;
        [Dependency] private readonly MapLoaderSystem _map = default!;
        [Dependency] private readonly ShipyardConsoleSystem _shipyardConsole = default!;

        public MapId? ShipyardMap { get; private set; }
        private float _shuttleIndex;
        private const float ShuttleSpawnBuffer = 1f;
        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            _sawmill = Logger.GetSawmill("shipyard");
            _shipyardConsole.InitializeConsole();
            SubscribeLocalEvent<ShipyardConsoleComponent, ComponentInit>(OnShipyardStartup);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        private void OnShipyardStartup(EntityUid uid, ShipyardConsoleComponent component, ComponentInit args)
        {
            SetupShipyard();
        }

        private void OnRoundRestart(RoundRestartCleanupEvent ev)
        {
            CleanupShipyard();
        }

        /// <summary>
        /// Adds a ship to the shipyard, calculates its price, and attempts to ftl-dock it to the given station
        /// </summary>
        /// <param name="stationUid">The ID of the station to dock the shuttle to</param>
        /// <param name="shuttlePath">The path to the grid file to load. Must be a grid file!</param>
        public void PurchaseShuttle(EntityUid? stationUid, string shuttlePath, out ShuttleComponent? vessel)
        {
            if (!TryComp<StationDataComponent>(stationUid, out var stationData) || !TryComp<ShuttleComponent>(AddShuttle(shuttlePath), out var shuttle))
            {
                vessel = null;
                return;
            }

            var targetGrid = _station.GetLargestGrid(stationData);

            if (targetGrid == null)
            {
                vessel = null;
                return;
            }

            var price = _pricing.AppraiseGrid(shuttle.Owner, null);

            //can do FTLTravel later instead if we want to open that door
            _shuttle.TryFTLDock(shuttle, targetGrid.Value);
            vessel = shuttle;
            _sawmill.Info($"Shuttle {shuttlePath} was purchased at {targetGrid} for {price}");
        }

        /// <summary>
        /// Loads a paused shuttle into the ShipyardMap from a file path
        /// </summary>
        /// <param name="shuttlePath">The path to the grid file to load. Must be a grid file!</param>
        /// <returns>Returns the EntityUid of the shuttle</returns>
        private EntityUid? AddShuttle(string shuttlePath)
        {
            if (ShipyardMap == null)
                return null;

            var loadOptions = new MapLoadOptions()
            {
                Offset = (500f + _shuttleIndex, 0f)
            };

            if (!_map.TryLoad(ShipyardMap.Value, shuttlePath.ToString(), out var gridList, loadOptions) || gridList == null)
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
                return null;
            };

            _shuttleIndex += _mapManager.GetGrid(gridList[0]).LocalAABB.Width + ShuttleSpawnBuffer;
            var actualGrids = new List<EntityUid>();
            var gridQuery = GetEntityQuery<MapGridComponent>();

            foreach (var ent in gridList)
            {
                if (!gridQuery.HasComponent(ent))
                    continue;

                actualGrids.Add(ent);
            };

            //only dealing with 1 grid at a time for now, until more is known about multi-grid drifting
            if (actualGrids.Count != 1)
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
                return null;
            };

            return actualGrids[0];
        }

        /// <summary>
        /// Checks a shuttle to make sure that it is docked to the given station, and that there are no lifeforms aboard. Then it appraises the grid, outputs to the server log, and deletes the grid
        /// </summary>
        /// <param name="stationUid">The ID of the station that the shuttle is docked to</param>
        /// <param name="shuttleUid">The grid ID of the shuttle to be appraised and sold</param>
        public void SellShuttle(EntityUid stationUid, EntityUid shuttleUid, out int bill)
        {
            bill = 0;

            if (!TryComp<StationDataComponent>(stationUid, out var stationGrid) || !HasComp<ShuttleComponent>(shuttleUid) || !TryComp<TransformComponent>(shuttleUid, out var xform) || ShipyardMap == null)
                return;

            var targetGrid = _station.GetLargestGrid(stationGrid);

            if (targetGrid == null)
                return;

            var gridDocks = _shuttle.GetDocks((EntityUid) targetGrid);
            var shuttleDocks = _shuttle.GetDocks(shuttleUid);
            var isDocked = false;

            foreach (var shuttleDock in shuttleDocks)
            {
                foreach (var gridDock in gridDocks)
                {
                    if (shuttleDock.DockedWith == gridDock.Owner)
                    {
                        isDocked = true;
                        break;
                    };
                };
                if (isDocked)
                    break;
            };

            if (!isDocked)
            {
                _sawmill.Warning($"shuttle is not docked to that station");
                return;
            };

            var mobQuery = GetEntityQuery<MobStateComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();

            if (_cargo.FoundOrganics(shuttleUid, mobQuery, xformQuery))
            {
                _sawmill.Warning($"organics on board");
                return;
            };

            //just yeet and delete for now. Might want to split it into another function later to send back to the shipyard map first to pause for something
            //also superman 3 moment
            bill = (int) _pricing.AppraiseGrid(shuttleUid);
            _mapManager.DeleteGrid(shuttleUid);
            _sawmill.Info($"Sold shuttle {shuttleUid} for {bill}");
            return;
        }

        private void CleanupShipyard()
        {
            if (ShipyardMap == null || !_mapManager.MapExists(ShipyardMap.Value))
            {
                ShipyardMap = null;
                return;
            };

            _mapManager.DeleteMap(ShipyardMap.Value);
        }

        private void SetupShipyard()
        {
            if (ShipyardMap != null && _mapManager.MapExists(ShipyardMap.Value))
                return;

            ShipyardMap = _mapManager.CreateMap();

            _mapManager.SetMapPaused(ShipyardMap.Value, true);
        }
    }
}
