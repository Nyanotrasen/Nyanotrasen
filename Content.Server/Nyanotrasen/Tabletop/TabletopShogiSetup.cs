using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Tabletop
{
    [UsedImplicitly]
    public sealed class TabletopShogiSetup : TabletopSetup
    {
        [DataField("boardPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ChessBoardPrototype { get; } = "ChessBoardTabletop";

        private Angle North = Angle.FromDegrees(180);

        public override void SetupTabletop(TabletopSession session, IEntityManager entityManager)
        {
            var chessboard = entityManager.SpawnEntity(ChessBoardPrototype, session.Position.Offset(-1, 0));

            session.Entities.Add(chessboard);

            SpawnPieces(session, entityManager, session.Position.Offset(-4.5f, 3.5f));
        }

        private void SpawnPieces(TabletopSession session, IEntityManager entityManager, MapCoordinates topLeft, float separation = 1f)
        {
            var (mapId, x, y) = topLeft;

            var transform = entityManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

            const string piecesBack       = "lnsgkgsnl";
            const string piecesRookBishop = " b     r ";
            const string piecesPawns      = "ppppppppp";

            SpawnPiecesRow(session, entityManager, transform, false, piecesBack, topLeft, separation);
            SpawnPiecesRow(session, entityManager, transform, false, piecesRookBishop, new MapCoordinates(x, y - 1 * separation, mapId), separation);
            SpawnPiecesRow(session, entityManager, transform, false, piecesPawns, new MapCoordinates(x, y - 2 * separation, mapId), separation);

            SpawnPiecesRow(session, entityManager, transform, true, piecesPawns, new MapCoordinates(x, y - 6 * separation, mapId), separation);
            SpawnPiecesRow(session, entityManager, transform, true, piecesRookBishop, new MapCoordinates(x, y - 7 * separation, mapId), separation);
            SpawnPiecesRow(session, entityManager, transform, true, piecesBack, new MapCoordinates(x, y - 8 * separation, mapId), separation);
        }

        private void SpawnPiecesRow(TabletopSession session, IEntityManager entityManager, SharedTransformSystem transform, bool black, string piecesRow, MapCoordinates left, float separation = 1f)
        {
            var (mapId, x, y) = left;

            if (black)
            {
                char[] charArray = piecesRow.ToCharArray();
                Array.Reverse(charArray);
                piecesRow = new string(charArray);
            }

            for (int i = 0; i < piecesRow.Length; i++)
            {
                EntityUid? spawned = null;

                switch (piecesRow[i])
                {
                    case 'p':
                        spawned = entityManager.SpawnEntity("ShogiPawn", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'l':
                        spawned = entityManager.SpawnEntity("ShogiRook", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'n':
                        spawned = entityManager.SpawnEntity("ShogiKnight", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 's':
                        spawned = entityManager.SpawnEntity("ShogiSilver", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'g':
                        spawned = entityManager.SpawnEntity("ShogiGold", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'r':
                        spawned = entityManager.SpawnEntity("ShogiRook", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'b':
                        spawned = entityManager.SpawnEntity("ShogiBishop", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                    case 'k':
                        spawned = entityManager.SpawnEntity(black ? "ShogiKingJeweled" : "ShogiKing", new MapCoordinates(x + i * separation, y, mapId));
                        break;
                }

                if (spawned == null)
                    continue;

                session.Entities.Add(spawned.Value);

                if (black)
                    transform.SetWorldRotation(spawned.Value, North);
            }
        }
    }
}
