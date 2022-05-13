using Content.Server.Coordinates.Helpers;
using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class SnapToGrid : IGraphAction
    {
        [DataField("southRotation")] public bool SouthRotation { get; private set; } = false;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            var transform = entityManager.GetComponent<TransformComponent>(uid);
            transform.Coordinates = transform.Coordinates.SnapToGrid(entityManager);

            if (SouthRotation)
            {
                transform.LocalRotation = Angle.Zero;
            }
        }
    }
}
