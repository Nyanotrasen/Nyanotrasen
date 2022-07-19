namespace Content.Server.Lamiae
{
    /// <summary>
    /// Controls initialization of the multisegmented lamia species.
    /// </summary>
    [RegisterComponent]
    public sealed class LamiaComponent : Component
    {
        public List<EntityUid> Segments = new();
    }
}
