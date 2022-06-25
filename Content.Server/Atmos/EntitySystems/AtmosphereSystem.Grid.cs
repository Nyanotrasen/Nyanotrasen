using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Reactions;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Atmos;
using Content.Shared.Maps;
// ReSharper disable once RedundantUsingDirective
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly GasTileOverlaySystem _gasTileOverlaySystem = default!;

        private void InitializeGrid()
        {
            SubscribeLocalEvent<GridAtmosphereComponent, ComponentInit>(OnGridAtmosphereInit);
            SubscribeLocalEvent<GridAtmosphereComponent, GridSplitEvent>(OnGridSplit);
        }

        private void OnGridAtmosphereInit(EntityUid uid, GridAtmosphereComponent gridAtmosphere, ComponentInit args)
        {
            base.Initialize();

            gridAtmosphere.Tiles.Clear();

            if (!TryComp(uid, out IMapGridComponent? mapGrid))
                return;

            if (gridAtmosphere.TilesUniqueMixes != null)
            {
                foreach (var (indices, mix) in gridAtmosphere.TilesUniqueMixes)
                {
                    try
                    {
                        gridAtmosphere.Tiles.Add(indices, new TileAtmosphere(mapGrid.Owner, indices, (GasMixture) gridAtmosphere.UniqueMixes![mix].Clone()));
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Logger.Error($"Error during atmos serialization! Tile at {indices} points to an unique mix ({mix}) out of range!");
                        throw;
                    }

                    InvalidateTile(gridAtmosphere, indices);
                }
            }

            GridRepopulateTiles(mapGrid.Grid, gridAtmosphere);
        }

        private void OnGridSplit(EntityUid uid, GridAtmosphereComponent originalGridAtmos, ref GridSplitEvent args)
        {
            foreach (var newGrid in args.NewGrids)
            {
                // Make extra sure this is a valid grid.
                if (!_mapManager.TryGetGrid(newGrid, out var mapGrid))
                    continue;

                var entity = mapGrid.GridEntityId;

                // If the new split grid has an atmosphere already somehow, use that. Otherwise, add a new one.
                if (!TryComp(entity, out GridAtmosphereComponent? newGridAtmos))
                    newGridAtmos = AddComp<GridAtmosphereComponent>(entity);

                // We assume the tiles on the new grid have the same coordinates as they did on the old grid...
                var enumerator = mapGrid.GetAllTilesEnumerator();

                while (enumerator.MoveNext(out var tile))
                {
                    var indices = tile.Value.GridIndices;

                    // This split event happens *before* the spaced tiles have been invalidated, therefore we can still
                    // access their gas data. On the next atmos update tick, these tiles will be spaced. Poof!
                    if (!originalGridAtmos.Tiles.TryGetValue(indices, out var tileAtmosphere))
                        continue;

                    // The new grid atmosphere has been initialized, meaning it has all the needed TileAtmospheres...
                    if (!newGridAtmos.Tiles.TryGetValue(indices, out var newTileAtmosphere))
                        // Let's be honest, this is really not gonna happen, but just in case...!
                        continue;

                    // Copy a bunch of data over... Not great, maybe put this in TileAtmosphere?
                    newTileAtmosphere.Air = tileAtmosphere.Air?.Clone() ?? null;
                    newTileAtmosphere.Hotspot = tileAtmosphere.Hotspot;
                    newTileAtmosphere.HeatCapacity = tileAtmosphere.HeatCapacity;
                    newTileAtmosphere.Temperature = tileAtmosphere.Temperature;
                    newTileAtmosphere.PressureDifference = tileAtmosphere.PressureDifference;
                    newTileAtmosphere.PressureDirection = tileAtmosphere.PressureDirection;

                    // TODO ATMOS: Somehow force GasTileOverlaySystem to perform an update *right now, right here.*
                    // The reason why is that right now, gas will flicker until the next GasTileOverlay update.
                    // That looks bad, of course. We want to avoid that! Anyway that's a bit more complicated so out of scope.

                    // Invalidate the tile, it's redundant but redundancy is good! Also HashSet so really, no duplicates.
                    InvalidateTile(originalGridAtmos, indices);
                    InvalidateTile(newGridAtmos, indices);
                }
            }
        }

        #region Grid Is Simulated

        /// <summary>
        ///     Returns whether a grid has a simulated atmosphere.
        /// </summary>
        /// <param name="coordinates">Coordinates to be checked.</param>
        /// <returns>Whether the grid has a simulated atmosphere.</returns>
        public bool IsSimulatedGrid(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsSimulatedGrid(tuple.Value.Grid);

            return false;
        }

        /// <summary>
        ///     Returns whether a grid has a simulated atmosphere.
        /// </summary>
        /// <param name="grid">Grid to be checked.</param>
        /// <returns>Whether the grid has a simulated atmosphere.</returns>
        public bool IsSimulatedGrid(EntityUid? grid)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            if (HasComp<GridAtmosphereComponent>(mapGrid.GridEntityId))
                return true;

            return false;
        }

        #endregion

        #region Grid Get All Mixtures

        /// <summary>
        ///     Gets all tile mixtures within a grid atmosphere, optionally invalidating them all.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the grid to get all tile mixtures from.</param>
        /// <param name="invalidate">Whether to invalidate all tiles.</param>
        /// <returns>All tile mixtures in a grid.</returns>
        public IEnumerable<GasMixture> GetAllTileMixtures(EntityCoordinates coordinates, bool invalidate = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAllTileMixtures(tuple.Value.Grid, invalidate);

            return Enumerable.Empty<GasMixture>();
        }

        /// <summary>
        ///     Gets all tile mixtures within a grid atmosphere, optionally invalidating them all.
        /// </summary>
        /// <param name="grid">Grid where to get all tile mixtures from.</param>
        /// <param name="invalidate">Whether to invalidate all tiles.</param>
        /// <returns>All tile mixtures in a grid.</returns>
        public IEnumerable<GasMixture> GetAllTileMixtures(EntityUid grid, bool invalidate = false)
        {
            // Return an array with a single space gas mixture for invalid grids.
            if (!grid.IsValid())
                return new []{ GasMixture.SpaceGas };

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Enumerable.Empty<GasMixture>();

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetAllTileMixtures(gridAtmosphere, invalidate);
            }

            return Enumerable.Empty<GasMixture>();
        }

        /// <summary>
        ///     Gets all tile mixtures within a grid atmosphere, optionally invalidating them all.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere to get all mixtures from.</param>
        /// <param name="invalidate">Whether to invalidate all mixtures.</param>
        /// <returns>All the tile mixtures in a grid.</returns>
        public IEnumerable<GasMixture> GetAllTileMixtures(GridAtmosphereComponent gridAtmosphere, bool invalidate = false)
        {
            foreach (var (indices, tile) in gridAtmosphere.Tiles)
            {
                if (tile.Air == null)
                    continue;

                if (invalidate)
                    InvalidateTile(gridAtmosphere, indices);

                yield return tile.Air;
            }
        }

        #endregion

        #region Grid Cell Volume

        /// <summary>
        ///     Gets the volume in liters for a number of tiles, on a specific grid.
        /// </summary>
        /// <param name="grid">The grid in question.</param>
        /// <param name="tiles">The amount of tiles.</param>
        /// <returns>The volume in liters that the tiles occupy.</returns>
        public float GetVolumeForTiles(EntityUid grid, int tiles = 1)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Atmospherics.CellVolume * tiles;

            return GetVolumeForTiles(mapGrid, tiles);

        }

        /// <summary>
        ///     Gets the volume in liters for a number of tiles, on a specific grid.
        /// </summary>
        /// <param name="mapGrid">The grid in question.</param>
        /// <param name="tiles">The amount of tiles.</param>
        /// <returns>The volume in liters that the tiles occupy.</returns>
        public float GetVolumeForTiles(IMapGrid mapGrid, int tiles = 1)
        {
            return Atmospherics.CellVolume * mapGrid.TileSize * tiles;

        }

        #endregion

        #region Grid Get Obstructing

        /// <summary>
        ///     Gets all obstructing AirtightComponent instances in a specific tile.
        /// </summary>
        /// <param name="mapGrid">The grid where to get the tile.</param>
        /// <param name="tile">The indices of the tile.</param>
        /// <returns></returns>
        public IEnumerable<AirtightComponent> GetObstructingComponents(IMapGrid mapGrid, Vector2i tile)
        {
            var airQuery = GetEntityQuery<AirtightComponent>();
            var enumerator = mapGrid.GetAnchoredEntitiesEnumerator(tile);

            while (enumerator.MoveNext(out var uid))
            {
                if (!airQuery.TryGetComponent(uid.Value, out var airtight)) continue;
                yield return airtight;
            }
        }

        public AtmosObstructionEnumerator GetObstructingComponentsEnumerator(IMapGrid mapGrid, Vector2i tile)
        {
            var ancEnumerator = mapGrid.GetAnchoredEntitiesEnumerator(tile);
            var airQuery = GetEntityQuery<AirtightComponent>();

            var enumerator = new AtmosObstructionEnumerator(ancEnumerator, airQuery);
            return enumerator;
        }

        private AtmosDirection GetBlockedDirections(IMapGrid mapGrid, Vector2i indices)
        {
            var value = AtmosDirection.Invalid;
            var enumerator = GetObstructingComponentsEnumerator(mapGrid, indices);

            while (enumerator.MoveNext(out var airtightComponent))
            {
                if (airtightComponent.AirBlocked)
                    value |= airtightComponent.AirBlockedDirection;
            }

            return value;
        }

        #endregion

        #region Grid Repopulate

        /// <summary>
        ///     Repopulates all tiles on a grid atmosphere.
        /// </summary>
        /// <param name="mapGrid">The grid where to get all valid tiles from.</param>
        /// <param name="gridAtmosphere">The grid atmosphere where the tiles will be repopulated.</param>
        public void GridRepopulateTiles(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere)
        {
            var volume = GetVolumeForTiles(mapGrid, 1);

            foreach (var tile in mapGrid.GetAllTiles())
            {
                if(!gridAtmosphere.Tiles.ContainsKey(tile.GridIndices))
                    gridAtmosphere.Tiles[tile.GridIndices] = new TileAtmosphere(tile.GridUid, tile.GridIndices, new GasMixture(volume){Temperature = Atmospherics.T20C});

                InvalidateTile(gridAtmosphere, tile.GridIndices);
            }

            foreach (var (position, tile) in gridAtmosphere.Tiles.ToArray())
            {
                UpdateAdjacent(mapGrid, gridAtmosphere, tile);
                InvalidateVisuals(mapGrid.GridEntityId, position);
            }
        }

        #endregion

        #region Tile Pry

        /// <summary>
        ///     Pries a tile in a grid.
        /// </summary>
        /// <param name="mapGrid">The grid in question.</param>
        /// <param name="tile">The indices of the tile.</param>
        private void PryTile(IMapGrid mapGrid, Vector2i tile)
        {
            if (!mapGrid.TryGetTileRef(tile, out var tileRef))
                return;

            tileRef.PryTile(_mapManager, _tileDefinitionManager, EntityManager, _robustRandom);
        }

        #endregion

        #region Tile Invalidate

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="coordinates">Coordinates of the tile.</param>
        public void InvalidateTile(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                InvalidateTile(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="grid">Grid where to invalidate the tile.</param>
        /// <param name="tile">The indices of the tile.</param>
        public void InvalidateTile(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                InvalidateTile(gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Invalidates a tile at a certain position.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to invalidate the tile.</param>
        /// <param name="tile">The tile's indices.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvalidateTile(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            gridAtmosphere.InvalidatedCoords.Add(tile);
        }

        #endregion

        #region Tile Invalidate Visuals

        public void InvalidateVisuals(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                InvalidateVisuals(tuple.Value.Grid, tuple.Value.Tile);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvalidateVisuals(EntityUid grid, Vector2i tile)
        {
            _gasTileOverlaySystem.Invalidate(grid, tile);
        }

        #endregion

        #region Tile Atmosphere Get

        /// <summary>
        ///     Gets the tile atmosphere in a position, or null.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The Tile Atmosphere in the position, or null if not on a grid.</returns>
        public TileAtmosphere? GetTileAtmosphere(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetTileAtmosphere(tuple.Value.Grid, tuple.Value.Tile);

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position, or null.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The Tile Atmosphere in the position, or null.</returns>
        public TileAtmosphere? GetTileAtmosphere(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return null;

            if(TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetTileAtmosphere(gridAtmosphere, tile);
            }

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position, or null.
        /// </summary>
        /// <param name="gridAtmosphere">Grid atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The Tile Atmosphere in the position, or null.</returns>
        public TileAtmosphere? GetTileAtmosphere(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return tileAtmosphere;

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position and if not possible returns a space tile or null.
        /// </summary>
        /// <param name="coordinates">Coordinates of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The tile atmosphere of a specific position in a grid, a space tile atmosphere if the tile is space or null if not on a grid.</returns>
        public TileAtmosphere? GetTileAtmosphereOrCreateSpace(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetTileAtmosphereOrCreateSpace(tuple.Value.Grid, tuple.Value.Tile);

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position and if not possible returns a space tile or null.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The tile atmosphere of a specific position in a grid, a space tile atmosphere if the tile is space or null if the grid doesn't exist.</returns>
        public TileAtmosphere? GetTileAtmosphereOrCreateSpace(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return null;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetTileAtmosphereOrCreateSpace(mapGrid, gridAtmosphere, tile);
            }

            return null;
        }

        /// <summary>
        ///     Gets the tile atmosphere in a position and if not possible returns a space tile or null.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <remarks>Do NOT use this outside of atmos internals.</remarks>
        /// <returns>The tile atmosphere of a specific position in a grid or a space tile atmosphere if the tile is space.</returns>
        public TileAtmosphere GetTileAtmosphereOrCreateSpace(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            var tileAtmosphere = GetTileAtmosphere(gridAtmosphere, tile);

            // Please note, you might run into a race condition when using this or GetTileAtmosphere.
            // The race condition occurs when a tile goes from being space to not-space, and then something
            // attempts to get the tile atmosphere for it before it has been revalidated by atmos.
            // The tile atmosphere will get revalidated on the next atmos tick, however.

            return tileAtmosphere ?? new TileAtmosphere(mapGrid.GridEntityId, tile, new GasMixture(Atmospherics.CellVolume) {Temperature = Atmospherics.TCMB}, true);
        }

        #endregion

        #region Tile Active Add

        /// <summary>
        ///     Makes a tile become active and start processing.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void AddActiveTile(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                AddActiveTile(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Makes a tile become active and start processing.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be activated.</param>
        public void AddActiveTile(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                AddActiveTile(gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Makes a tile become active and start processing.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be activated.</param>
        public void AddActiveTile(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            AddActiveTile(gridAtmosphere, tileAtmosphere);
        }

        /// <summary>
        ///     Makes a tile become active and start processing. Does NOT check if the tile belongs to the grid atmos.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Tile Atmosphere to be activated.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddActiveTile(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile)
        {
            if (tile.Air == null)
                return;

            tile.Excited = true;
            gridAtmosphere.ActiveTiles.Add(tile);
        }

        #endregion

        #region Tile Active Remove

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(EntityCoordinates coordinates, bool disposeExcitedGroup = true)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                RemoveActiveTile(tuple.Value.Grid, tuple.Value.Tile, disposeExcitedGroup);
        }

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be deactivated.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(EntityUid grid, Vector2i tile, bool disposeExcitedGroup = true)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                RemoveActiveTile(gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile to be deactivated.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        public void RemoveActiveTile(GridAtmosphereComponent gridAtmosphere, Vector2i tile, bool disposeExcitedGroup = true)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            RemoveActiveTile(gridAtmosphere, tileAtmosphere, disposeExcitedGroup);
        }

        /// <summary>
        ///     Makes a tile become inactive and stop processing.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Tile Atmosphere to be deactivated.</param>
        /// <param name="disposeExcitedGroup">Whether to dispose of the tile's <see cref="ExcitedGroup"/></param>
        private void RemoveActiveTile(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, bool disposeExcitedGroup = true)
        {
            tile.Excited = false;
            gridAtmosphere.ActiveTiles.Remove(tile);

            if (tile.ExcitedGroup == null)
                return;

            if (disposeExcitedGroup)
                ExcitedGroupDispose(gridAtmosphere, tile.ExcitedGroup);
            else
                ExcitedGroupRemoveTile(tile.ExcitedGroup, tile);
        }

        #endregion

        #region Tile Mixture

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        public GasMixture? GetTileMixture(EntityCoordinates coordinates, bool invalidate = false)
        {
            return TryGetGridAndTile(coordinates, out var tuple)
                ? GetTileMixture(tuple.Value.Grid, tuple.Value.Tile, invalidate) : GasMixture.SpaceGas;
        }

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="grid">Grid where to get the tile air.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        public GasMixture? GetTileMixture(EntityUid grid, Vector2i tile, bool invalidate = false)
        {
            // Always return space gas mixtures for invalid grids (grid 0)
            if (!grid.IsValid())
                return GasMixture.SpaceGas;

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return null;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetTileMixture(gridAtmosphere, tile, invalidate);
            }

            if (TryComp(mapGrid.GridEntityId, out SpaceAtmosphereComponent? _))
            {
                // Always return a new space gas mixture in this case.
                return GasMixture.SpaceGas;
            }

            return null;
        }

        /// <summary>
        ///     Returns a reference to the gas mixture on a tile, or null.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile air.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="invalidate">Whether to invalidate the tile.</param>
        /// <returns>The tile mixture, or null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasMixture? GetTileMixture(GridAtmosphereComponent gridAtmosphere, Vector2i tile, bool invalidate = false)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return null;

            // Invalidate the tile if needed.
            if (invalidate)
                InvalidateTile(gridAtmosphere, tile);

            // Return actual tile air or null.
            return tileAtmosphere.Air;
        }

        #endregion

        #region Tile React

        /// <summary>
        ///     Causes a gas mixture reaction on a specific tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <returns>Reaction results.</returns>
        public ReactionResult React(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return React(tuple.Value.Grid, tuple.Value.Tile);

            return ReactionResult.NoReaction;
        }

        /// <summary>
        ///     Causes a gas mixture reaction on a specific tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <returns>Reaction results.</returns>
        public ReactionResult React(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return ReactionResult.NoReaction;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return React(gridAtmosphere, tile);
            }

            return ReactionResult.NoReaction;
        }

        /// <summary>
        ///     Causes a gas mixture reaction on a specific tile.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <returns>Reaction results.</returns>
        public ReactionResult React(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere) || tileAtmosphere.Air == null)
                return ReactionResult.NoReaction;

            InvalidateTile(gridAtmosphere, tile);

            return React(tileAtmosphere.Air, tileAtmosphere);
        }

        #endregion

        #region Tile Air-blocked

        /// <summary>
        ///     Returns if the tile in question is "air-blocked" in a certain direction or not.
        ///     This could be due to a number of reasons, such as walls, doors, etc.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="direction">Directions to check.</param>
        /// <returns>Whether the tile is blocked in the directions specified.</returns>
        public bool IsTileAirBlocked(EntityCoordinates coordinates, AtmosDirection direction = AtmosDirection.All)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsTileAirBlocked(tuple.Value.Grid, tuple.Value.Tile, direction);

            return false;
        }

        /// <summary>
        ///     Returns if the tile in question is "air-blocked" in a certain direction or not.
        ///     This could be due to a number of reasons, such as walls, doors, etc.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="direction">Directions to check.</param>
        /// <returns>Whether the tile is blocked in the directions specified.</returns>
        public bool IsTileAirBlocked(EntityUid grid, Vector2i tile, AtmosDirection direction = AtmosDirection.All)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            return IsTileAirBlocked(mapGrid, tile, direction);
        }

        /// <summary>
        ///     Returns if the tile in question is "air-blocked" in a certain direction or not.
        ///     This could be due to a number of reasons, such as walls, doors, etc.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="direction">Directions to check.</param>
        /// <returns>Whether the tile is blocked in the directions specified.</returns>
        public bool IsTileAirBlocked(IMapGrid mapGrid, Vector2i tile, AtmosDirection direction = AtmosDirection.All)
        {
            var directions = AtmosDirection.Invalid;

            var enumerator = GetObstructingComponentsEnumerator(mapGrid, tile);

            while (enumerator.MoveNext(out var obstructingComponent))
            {
                if (!obstructingComponent.AirBlocked)
                    continue;

                // We set the directions that are air-blocked so far,
                // as you could have a full obstruction with only 4 directional air blockers.
                directions |= obstructingComponent.AirBlockedDirection;

                if (directions.IsFlagSet(direction))
                    return true;
            }

            return false;
        }

        #endregion

        #region Tile Space

        /// <summary>
        ///     Returns whether the specified tile is a space tile or not.
        /// </summary>
        /// <param name="coordinates">Coordinates where to check the tile.</param>
        /// <returns>Whether the tile is space or not.</returns>
        public bool IsTileSpace(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsTileSpace(tuple.Value.Grid, tuple.Value.Tile);

            return true;
        }

        /// <summary>
        ///     Returns whether the specified tile is a space tile or not.
        /// </summary>
        /// <param name="grid">Grid where to check the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <returns>Whether the tile is space or not.</returns>
        public bool IsTileSpace(EntityUid grid, Vector2i tile)
        {
            return !_mapManager.TryGetGrid(grid, out var mapGrid) || IsTileSpace(mapGrid, tile);
        }

        public bool IsTileSpace(IMapGrid mapGrid, Vector2i tile)
        {
            if (!mapGrid.TryGetTileRef(tile, out var tileRef))
                return true;

            return ((ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId]).IsSpace;
        }

        #endregion

        #region Tile Get Heat Capacity

        /// <summary>
        ///     Get a tile's heat capacity, based on the tile type, tile contents and tile gas mixture.
        /// </summary>
        public float GetTileHeatCapacity(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetTileHeatCapacity(tuple.Value.Grid, tuple.Value.Tile);

            return Atmospherics.MinimumHeatCapacity;
        }

        /// <summary>
        ///     Get a tile's heat capacity, based on the tile type, tile contents and tile gas mixture.
        /// </summary>
        public float GetTileHeatCapacity(EntityUid grid, Vector2i tile)
        {
            // Always return space gas mixtures for invalid grids (grid 0)
            if (!grid.IsValid())
                return Atmospherics.MinimumHeatCapacity;

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Atmospherics.MinimumHeatCapacity;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetTileHeatCapacity(gridAtmosphere, tile);
            }

            if (TryComp(mapGrid.GridEntityId, out SpaceAtmosphereComponent? _))
            {
                return Atmospherics.SpaceHeatCapacity;
            }

            return Atmospherics.MinimumHeatCapacity;
        }

        /// <summary>
        ///     Get a tile's heat capacity, based on the tile type, tile contents and tile gas mixture.
        /// </summary>
        public float GetTileHeatCapacity(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return Atmospherics.MinimumHeatCapacity;

            return GetTileHeatCapacity(tileAtmosphere);
        }

        /// <summary>
        ///     Get a tile's heat capacity, based on the tile type, tile contents and tile gas mixture.
        /// </summary>
        public float GetTileHeatCapacity(TileAtmosphere tile)
        {
            return tile.HeatCapacity + (tile.Air == null ? 0 : GetHeatCapacity(tile.Air));
        }

        #endregion

        #region Adjacent Get Positions

        /// <summary>
        ///     Gets all the positions adjacent to a tile. Can include air-blocked directions.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <returns>The positions adjacent to the tile.</returns>
        public IEnumerable<Vector2i> GetAdjacentTiles(EntityCoordinates coordinates, bool includeBlocked = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAdjacentTiles(tuple.Value.Grid, tuple.Value.Tile, includeBlocked);

            return Enumerable.Empty<Vector2i>();
        }

        /// <summary>
        ///     Gets all the positions adjacent to a tile. Can include air-blocked directions.
        /// </summary>
        /// <param name="grid">Grid where to get the tiles.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <returns>The positions adjacent to the tile.</returns>
        public IEnumerable<Vector2i> GetAdjacentTiles(EntityUid grid, Vector2i tile, bool includeBlocked = false)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Enumerable.Empty<Vector2i>();

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetAdjacentTiles(gridAtmosphere, tile, includeBlocked);
            }

            return Enumerable.Empty<Vector2i>();
        }

        /// <summary>
        ///     Gets all the positions adjacent to a tile. Can include air-blocked directions.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tiles.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <returns>The positions adjacent to the tile.</returns>
        public IEnumerable<Vector2i> GetAdjacentTiles(GridAtmosphereComponent gridAtmosphere, Vector2i tile, bool includeBlocked = false)
        {
            if(!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                yield break;

            for (var i = 0; i < tileAtmosphere.AdjacentTiles.Length; i++)
            {
                var adjacentTile = tileAtmosphere.AdjacentTiles[i];
                // TileAtmosphere has nullable disabled, so just in case...
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (adjacentTile?.Air == null)
                    continue;

                if (!includeBlocked)
                {
                    var direction = (AtmosDirection) (1 << i);
                    if (tileAtmosphere.BlockedAirflow.IsFlagSet(direction))
                        continue;
                }

                yield return adjacentTile.GridIndices;
            }
        }

        #endregion

        #region Adjacent Get Mixture

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        public IEnumerable<GasMixture> GetAdjacentTileMixtures(EntityCoordinates coordinates, bool includeBlocked = false, bool invalidate = false)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return GetAdjacentTileMixtures(tuple.Value.Grid, tuple.Value.Tile, includeBlocked, invalidate);

            return Enumerable.Empty<GasMixture>();
        }

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        public IEnumerable<GasMixture> GetAdjacentTileMixtures(EntityUid grid, Vector2i tile, bool includeBlocked = false, bool invalidate = false)
        {
            // For invalid grids, return an array with a single space gas mixture in it.
            if (!grid.IsValid())
                return new []{ GasMixture.SpaceGas };

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return Enumerable.Empty<GasMixture>();

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return GetAdjacentTileMixtures(gridAtmosphere, tile, includeBlocked, invalidate);
            }

            return Enumerable.Empty<GasMixture>();
        }

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        public IEnumerable<GasMixture> GetAdjacentTileMixtures(GridAtmosphereComponent gridAtmosphere, Vector2i tile, bool includeBlocked = false, bool invalidate = false)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return Enumerable.Empty<GasMixture>();

            return GetAdjacentTileMixtures(gridAtmosphere, tileAtmosphere, includeBlocked, invalidate);
        }

        /// <summary>
        ///     Gets all tile gas mixtures adjacent to a specific tile, and optionally invalidates them.
        ///     Does not return the tile in question, only the adjacent ones.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where the tile is.</param>
        /// <param name="tile">Tile Atmosphere in question.</param>
        /// <param name="includeBlocked">Whether to include tiles in directions the tile is air-blocked in.</param>
        /// <param name="invalidate">Whether to invalidate all adjacent tiles.</param>
        /// <returns>All adjacent tile gas mixtures to the tile in question</returns>
        private IEnumerable<GasMixture> GetAdjacentTileMixtures(GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, bool includeBlocked = false, bool invalidate = false)
        {
            for (var i = 0; i < tile.AdjacentTiles.Length; i++)
            {
                var adjacentTile = tile.AdjacentTiles[i];

                // TileAtmosphere has nullable disabled, so just in case...
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (adjacentTile?.Air == null)
                    continue;

                if (!includeBlocked)
                {
                    var direction = (AtmosDirection) (1 << i);
                    if (tile.BlockedAirflow.IsFlagSet(direction))
                        continue;
                }

                if (invalidate)
                    InvalidateTile(gridAtmosphere, adjacentTile.GridIndices);

                yield return adjacentTile.Air;
            }
        }

        #endregion

        #region Adjacent Update

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void UpdateAdjacent(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                UpdateAdjacent(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void UpdateAdjacent(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                UpdateAdjacent(mapGrid, gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void UpdateAdjacent(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            UpdateAdjacent(mapGrid, gridAtmosphere, tileAtmosphere);
        }

        /// <summary>
        ///     Immediately updates a tile's blocked air directions.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid Atmosphere of the tile.</param>
        /// <param name="tileAtmosphere">Tile Atmosphere to be updated.</param>
        private void UpdateAdjacent(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, TileAtmosphere tileAtmosphere)
        {
            tileAtmosphere.AdjacentBits = AtmosDirection.Invalid;
            tileAtmosphere.BlockedAirflow = GetBlockedDirections(mapGrid, tileAtmosphere.GridIndices);

            for (var i = 0; i < Atmospherics.Directions; i++)
            {
                var direction = (AtmosDirection) (1 << i);

                var otherIndices = tileAtmosphere.GridIndices.Offset(direction);

                var adjacent = GetTileAtmosphereOrCreateSpace(mapGrid, gridAtmosphere, otherIndices);
                tileAtmosphere.AdjacentTiles[direction.ToIndex()] = adjacent;

                UpdateAdjacent(mapGrid, gridAtmosphere, adjacent, direction.GetOpposite());

                if (!tileAtmosphere.BlockedAirflow.IsFlagSet(direction)
                    && !IsTileAirBlocked(mapGrid, adjacent.GridIndices, direction.GetOpposite()))
                {
                    tileAtmosphere.AdjacentBits |= direction;
                }
            }

            if (!tileAtmosphere.AdjacentBits.IsFlagSet(tileAtmosphere.MonstermosInfo.CurrentTransferDirection))
                tileAtmosphere.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
        }

        /// <summary>
        ///     Immediately updates a tile's single blocked air direction.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="direction">Direction to be updated.</param>
        public void UpdateAdjacent(EntityCoordinates coordinates, AtmosDirection direction)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                UpdateAdjacent(tuple.Value.Grid, tuple.Value.Tile, direction);
        }

        /// <summary>
        ///     Immediately updates a tile's single blocked air direction.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="direction">Direction to be updated.</param>
        public void UpdateAdjacent(EntityUid grid, Vector2i tile, AtmosDirection direction)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                UpdateAdjacent(mapGrid, gridAtmosphere, tile, direction);
                return;
            }
        }

        /// <summary>
        ///     Immediately updates a tile's single blocked air direction.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="direction">Direction to be updated.</param>
        public void UpdateAdjacent(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, Vector2i tile, AtmosDirection direction)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            UpdateAdjacent(mapGrid, gridAtmosphere, tileAtmosphere, direction);
        }

        /// <summary>
        ///     Immediately updates a tile's single blocked air direction.
        /// </summary>
        /// <param name="mapGrid">Grid where to get the tile.</param>
        /// <param name="gridAtmosphere">Grid where to get the tile.</param>
        /// <param name="tile">Tile Atmosphere to be updated.</param>
        /// <param name="direction">Direction to be updated.</param>
        private void UpdateAdjacent(IMapGrid mapGrid, GridAtmosphereComponent gridAtmosphere, TileAtmosphere tile, AtmosDirection direction)
        {
            tile.AdjacentTiles[direction.ToIndex()] = GetTileAtmosphereOrCreateSpace(mapGrid, gridAtmosphere, tile.GridIndices.Offset(direction));

            if (!tile.BlockedAirflow.IsFlagSet(direction) && !IsTileAirBlocked(mapGrid, tile.GridIndices.Offset(direction), direction.GetOpposite()))
            {
                tile.AdjacentBits |= direction;
            }
            else
            {
                tile.AdjacentBits &= ~direction;
            }

            if (!tile.AdjacentBits.IsFlagSet(tile.MonstermosInfo.CurrentTransferDirection))
                tile.MonstermosInfo.CurrentTransferDirection = AtmosDirection.Invalid;
        }

        #endregion

        #region Hotspot Expose

        /// <summary>
        ///     Exposes temperature to a tile, creating a hotspot (fire) if the conditions are ideal.
        ///     Can also be used to make an existing hotspot hotter/bigger. Also invalidates the tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <param name="exposedTemperature">Temperature to expose to the tile.</param>
        /// <param name="exposedVolume">Volume of the exposed temperature.</param>
        /// <param name="soh">If true, the existing hotspot values will be set to the exposed values, but only if they're smaller.</param>
        public void HotspotExpose(EntityCoordinates coordinates, float exposedTemperature, float exposedVolume, bool soh = false)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                HotspotExpose(tuple.Value.Grid, tuple.Value.Tile, exposedTemperature, exposedVolume, soh);
        }

        /// <summary>
        ///     Exposes temperature to a tile, creating a hotspot (fire) if the conditions are ideal.
        ///     Can also be used to make an existing hotspot hotter/bigger. Also invalidates the tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <param name="exposedTemperature">Temperature to expose to the tile.</param>
        /// <param name="exposedVolume">Volume of the exposed temperature.</param>
        /// <param name="soh">If true, the existing hotspot values will be set to the exposed values, but only if they're smaller.</param>
        public void HotspotExpose(EntityUid grid, Vector2i tile, float exposedTemperature, float exposedVolume, bool soh = false)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                var tileAtmosphere = GetTileAtmosphere(gridAtmosphere, tile);

                if (tileAtmosphere == null)
                    return;

                HotspotExpose(gridAtmosphere, tileAtmosphere, exposedTemperature, exposedVolume, soh);
                InvalidateTile(gridAtmosphere, tile);
                return;
            }
        }

        #endregion

        #region Hotspot Extinguish

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void HotspotExtinguish(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                HotspotExtinguish(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void HotspotExtinguish(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                HotspotExtinguish(gridAtmosphere, tile);
                return;
            }
        }

        /// <summary>
        ///     Extinguishes a hotspot (fire) on a certain tile, if any. Also invalidates the tile.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void HotspotExtinguish(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            tileAtmosphere.Hotspot = new Hotspot();
            InvalidateTile(gridAtmosphere, tile);
        }

        #endregion

        #region Hotspot Active

        /// <summary>
        ///     Returns whether there's an active hotspot (fire) on a certain tile.
        /// </summary>
        /// <param name="coordinates">Position where to get the tile.</param>
        /// <returns>Whether the hotspot is active or not.</returns>
        public bool IsHotspotActive(EntityCoordinates coordinates)
        {
            if (TryGetGridAndTile(coordinates, out var tuple))
                return IsHotspotActive(tuple.Value.Grid, tuple.Value.Tile);

            return false;
        }

        /// <summary>
        ///     Returns whether there's an active hotspot (fire) on a certain tile.
        /// </summary>
        /// <param name="grid">Grid where to get the tile</param>
        /// <param name="tile">Indices for the tile</param>
        /// <returns>Whether the hotspot is active or not.</returns>
        public bool IsHotspotActive(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                return IsHotspotActive(gridAtmosphere, tile);
            }

            return false;
        }

        /// <summary>
        ///     Returns whether there's an active hotspot (fire) on a certain tile.
        /// </summary>
        /// <param name="gridAtmosphere">Grid Atmosphere where to get the tile</param>
        /// <param name="tile">Indices for the tile</param>
        /// <returns>Whether the hotspot is active or not.</returns>
        public bool IsHotspotActive(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return false;

            return tileAtmosphere.Hotspot.Valid;
        }

        #endregion

        #region PipeNet Add

        public void AddPipeNet(PipeNet pipeNet)
        {
            if (!_mapManager.TryGetGrid(pipeNet.Grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                gridAtmosphere.PipeNets.Add(pipeNet);
            }
        }

        #endregion

        #region PipeNet Remove

        public void RemovePipeNet(PipeNet pipeNet)
        {
            if (!_mapManager.TryGetGrid(pipeNet.Grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                gridAtmosphere.PipeNets.Remove(pipeNet);
            }
        }

        #endregion

        #region AtmosDevice Add

        public bool AddAtmosDevice(AtmosDeviceComponent atmosDevice)
        {
            var grid = Comp<TransformComponent>(atmosDevice.Owner).GridUid;

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                atmosDevice.JoinedGrid = grid;
                gridAtmosphere.AtmosDevices.Add(atmosDevice);
                return true;
            }

            return false;
        }

        #endregion

        #region AtmosDevice Remove

        public bool RemoveAtmosDevice(AtmosDeviceComponent atmosDevice)
        {
            if (atmosDevice.JoinedGrid == null)
                return false;

            var grid = atmosDevice.JoinedGrid.Value;

            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return false;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere)
                && gridAtmosphere.AtmosDevices.Contains(atmosDevice))
            {
                atmosDevice.JoinedGrid = null;
                gridAtmosphere.AtmosDevices.Remove(atmosDevice);
                return true;
            }

            return false;
        }

        #endregion

        #region Mixture Safety

        /// <summary>
        ///     Checks whether a tile's gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        /// <returns>Whether the tile's gas mixture is probably safe.</returns>
        public bool IsTileMixtureProbablySafe(EntityCoordinates coordinates)
        {
            return IsMixtureProbablySafe(GetTileMixture(coordinates));
        }

        /// <summary>
        ///     Checks whether a tile's gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        /// <returns>Whether the tile's gas mixture is probably safe.</returns>
        public bool IsTileMixtureProbablySafe(EntityUid grid, Vector2i tile)
        {
            return IsMixtureProbablySafe(GetTileMixture(grid, tile));
        }

        /// <summary>
        ///     Checks whether a gas mixture is probably safe.
        ///     This only checks temperature and pressure, not gas composition.
        /// </summary>
        /// <param name="air">Mixture to be checked.</param>
        /// <returns>Whether the mixture is probably safe.</returns>
        public bool IsMixtureProbablySafe(GasMixture? air)
        {
            // Note that oxygen mix isn't checked, but survival boxes make that not necessary.
            if (air == null)
                return false;

            switch (air.Pressure)
            {
                case <= Atmospherics.WarningLowPressure:
                case >= Atmospherics.WarningHighPressure:
                    return false;
            }

            switch (air.Temperature)
            {
                case <= 260:
                case >= 360:
                    return false;
            }

            return true;
        }

        #endregion

        #region Fix Vacuum

        /// <summary>
        ///     Attempts to fix a sudden vacuum by creating gas based on adjacent tiles.
        /// </summary>
        /// <param name="coordinates">Coordinates where to get the tile.</param>
        public void FixVacuum(EntityCoordinates coordinates)
        {
            if(TryGetGridAndTile(coordinates, out var tuple))
                FixVacuum(tuple.Value.Grid, tuple.Value.Tile);
        }

        /// <summary>
        ///     Attempts to fix a sudden vacuum by creating gas based on adjacent tiles.
        /// </summary>
        /// <param name="grid">Grid where to get the tile.</param>
        /// <param name="tile">Indices of the tile.</param>
        public void FixVacuum(EntityUid grid, Vector2i tile)
        {
            if (!_mapManager.TryGetGrid(grid, out var mapGrid))
                return;

            if (TryComp(mapGrid.GridEntityId, out GridAtmosphereComponent? gridAtmosphere))
            {
                FixVacuum(gridAtmosphere, tile);
                return;
            }
        }

        public void FixVacuum(GridAtmosphereComponent gridAtmosphere, Vector2i tile)
        {
            if (!gridAtmosphere.Tiles.TryGetValue(tile, out var tileAtmosphere))
                return;

            var adjacent = GetAdjacentTileMixtures(gridAtmosphere, tileAtmosphere, false, true).ToArray();
            tileAtmosphere.Air = new GasMixture(GetVolumeForTiles(tileAtmosphere.GridIndex, 1))
                {Temperature = Atmospherics.T20C};

            // Return early, let's not cause any funny NaNs.
            if (adjacent.Length == 0)
                return;

            var ratio = 1f / adjacent.Length;
            var totalTemperature = 0f;

            foreach (var adj in adjacent)
            {
                totalTemperature += adj.Temperature;

                // Remove a bit of gas from the adjacent ratio...
                var mix = adj.RemoveRatio(ratio);

                // And merge it to the new tile air.
                Merge(tileAtmosphere.Air, mix);

                // Return removed gas to its original mixture.
                Merge(adj, mix);
            }

            // New temperature is the arithmetic mean of the sum of the adjacent temperatures...
            tileAtmosphere.Air.Temperature = totalTemperature / adjacent.Length;
        }

        public bool NeedsVacuumFixing(IMapGrid mapGrid, Vector2i indices)
        {
            var value = false;

            foreach (var airtightComponent in GetObstructingComponents(mapGrid, indices))
            {
                value |= airtightComponent.FixVacuum;
            }

            return value;
        }

        #endregion

        #region Position Helpers

        private TileRef? GetTile(TileAtmosphere tile)
        {
            return tile.GridIndices.GetTileRef(tile.GridIndex, _mapManager);
        }

        public bool TryGetGridAndTile(MapCoordinates coordinates, [NotNullWhen(true)] out (EntityUid Grid, Vector2i Tile)? tuple)
        {
            if (!_mapManager.TryFindGridAt(coordinates, out var grid))
            {
                tuple = null;
                return false;
            }

            tuple = (grid.GridEntityId, grid.TileIndicesFor(coordinates));
            return true;
        }

        public bool TryGetGridAndTile(EntityCoordinates coordinates, [NotNullWhen(true)] out (EntityUid Grid, Vector2i Tile)? tuple)
        {
            if (!coordinates.IsValid(EntityManager))
            {
                tuple = null;
                return false;
            }

            var gridId = coordinates.GetGridUid(EntityManager);

            if (!_mapManager.TryGetGrid(gridId, out var grid))
            {
                tuple = null;
                return false;
            }

            tuple = (gridId.Value, grid.TileIndicesFor(coordinates));
            return true;
        }

        public bool TryGetMapGrid(GridAtmosphereComponent gridAtmosphere, [NotNullWhen(true)] out IMapGrid? mapGrid)
        {
            if (TryComp(gridAtmosphere.Owner, out IMapGridComponent? mapGridComponent))
            {
                mapGrid = mapGridComponent.Grid;
                return true;
            }

            mapGrid = null;
            return false;
        }

        #endregion
    }
}
