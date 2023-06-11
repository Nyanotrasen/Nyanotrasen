using Robust.Client.Player;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Network.Messages;
using Robust.Shared.Network;
using Content.Shared.Coordinates;
using Content.Shared.Actions;

namespace Content.Client.Mapping;

public sealed partial class MappingSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _networkMan = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;

    private void InitializeCopyBuffers()
    {
        SubscribeLocalEvent<StartMapCopyActionEvent>(OnStartMapCopy);
        SubscribeLocalEvent<MapPasteBufferActionEvent>(OnMapPasteBuffer);
    }

    private void OnStartMapCopy(StartMapCopyActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _placementMan.BeginPlacing(new PlacementInformation()
        {
            IsTile = false,
            PlacementOption = "PlaceFree"
        }, new MapCopyPlacementHijack(
            EntityManager,
            _mapMan,
            _playerMan,
            _actionsSystem,
            _entityLookupSystem,
            _transformSystem,
            _spriteSystem));
    }

    private void OnMapPasteBuffer(MapPasteBufferActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Buffer == null)
            return;

        args.Handled = true;

        var gridId = args.Target.GetGridUid(EntityManager);
        if (gridId == null)
            return;

        var grid = _mapMan.GetGrid(gridId.Value);
        var startTile = grid.CoordinatesToTile(args.Target);
        var start = grid.ToCoordinates(startTile);

        // Place entities.
        foreach (var (offset, angle, proto) in args.Buffer.Entities)
        {
            var destination = start.Offset(offset);

            var message = new MsgPlacement
            {
                Align = "Default",
                PlaceType = PlacementManagerMessage.RequestPlacement,
                DirRcv = DirExt.Convert(DirExt.ToRsiDirection(angle, RSI.State.DirectionType.Dir8)),
                IsTile = false,
                Replacement = true,
                EntityTemplateName = proto,
                EntityCoordinates = destination
            };

            _networkMan.ClientSendMessage(message);
        }

        // Place tiles.
        foreach (var (offset, id) in args.Buffer.Tiles)
        {
            var destination = start.Offset(offset);

            var message = new MsgPlacement
            {
                PlaceType = PlacementManagerMessage.RequestPlacement,
                IsTile = true,
                TileType = id,
                EntityCoordinates = destination
            };

            _networkMan.ClientSendMessage(message);
        }
    }
}

public sealed class StartMapCopyActionEvent : InstantActionEvent { }

public sealed class MapCopyBuffer
{
    [DataField("entities")]
    public List<(Vector2 offset, Angle rotation, string proto)> Entities = new();

    [DataField("tiles")]
    public List<(Vector2i offset, ushort id)> Tiles = new();
}

public sealed class MapPasteBufferActionEvent : WorldTargetActionEvent
{
    [DataField("buffer")]
    public MapCopyBuffer? Buffer;
}

