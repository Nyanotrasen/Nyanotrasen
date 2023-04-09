using System.Threading;
using Robust.Shared.Map;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tools.Components;

namespace Content.Server.Tools;

public sealed partial class ToolSystem
{
    private void InitializeEarthDigging()
    {
        SubscribeLocalEvent<EarthDiggingComponent, AfterInteractEvent>(OnEarthDiggingAfterInteract);
        SubscribeLocalEvent<EarthDiggingComponent, EarthDiggingCompleteEvent>(OnEarthDigComplete);
        SubscribeLocalEvent<EarthDiggingComponent, EarthDiggingCancelledEvent>(OnEarthDigCancelled);
    }

    private void OnEarthDigCancelled(EntityUid uid, EarthDiggingComponent component, EarthDiggingCancelledEvent args)
    {
        if (component.CancelToken != null)
        {
            component.CancelToken.Cancel();
            component.CancelToken = null;
        }
    }

    private void OnEarthDigComplete(EntityUid uid, EarthDiggingComponent component, EarthDiggingCompleteEvent args)
    {
        var gridUid = args.Coordinates.GetGridUid(EntityManager);
        if (gridUid == null)
            return;

        var grid = _mapManager.GetGrid(gridUid.Value);
        var tile = grid.GetTileRef(args.Coordinates);

        if (_tileDefinitionManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || !tileDef.CanShovel
            || tileDef.BaseTurfs.Count == 0
            || tile.IsBlockedTurf(true))
        {
            return;
        }

        _tile.DigTile(tile);
    }

    private void OnEarthDiggingAfterInteract(EntityUid uid, EarthDiggingComponent component,
        AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != null)
            return;

        if (TryDig(args.User, uid, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryDig(EntityUid user, EntityUid shovel, EarthDiggingComponent component, EntityCoordinates clickLocation)
    {
        ToolComponent? tool = null;
        if (component.ToolComponentNeeded && !TryComp<ToolComponent?>(component.Owner, out tool))
            return false;

        if (!_mapManager.TryGetGrid(clickLocation.GetGridUid(EntityManager), out var mapGrid))
            return false;

        var tile = mapGrid.GetTileRef(clickLocation);

        var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

        if (!_interactionSystem.InRangeUnobstructed(user, coordinates, popup: false))
            return false;

        if (_tileDefinitionManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || !tileDef.CanShovel
            || tileDef.BaseTurfs.Count == 0
            || _tileDefinitionManager[tileDef.BaseTurfs[^1]] is not ContentTileDefinition newDef
            || tile.IsBlockedTurf(true))
        {
            return false;
        }

        var token = new CancellationTokenSource();

        var toolEvData = new ToolEventData(new EarthDiggingCompleteEvent(clickLocation, shovel), cancelledEv:new EarthDiggingCancelledEvent() ,targetEntity:shovel);

        if (!UseTool(shovel, user, null, component.Delay, new[] { component.QualityNeeded }, toolEvData, toolComponent: tool, cancelToken: component.CancelToken))
            return false;

        return true;
    }

    private sealed class EarthDiggingCompleteEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates { get; set; }
        public EntityUid Shovel;

        public EarthDiggingCompleteEvent(EntityCoordinates coordinates, EntityUid shovel)
        {
            Coordinates = coordinates;
            Shovel = shovel;
        }
    }

    private sealed class EarthDiggingCancelledEvent : EntityEventArgs
    {
        public EntityUid Shovel;
    }
}
