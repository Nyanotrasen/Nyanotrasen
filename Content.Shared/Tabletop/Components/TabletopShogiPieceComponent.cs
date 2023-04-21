namespace Content.Shared.Tabletop.Components
{
    [RegisterComponent]
    public sealed class TabletopShogiPieceComponent : Component
    {
        [ViewVariables]
        [DataField("canPromote")]
        public bool CanPromote = false;
    }
}
