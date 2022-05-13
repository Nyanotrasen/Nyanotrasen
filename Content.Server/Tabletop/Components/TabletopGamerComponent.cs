namespace Content.Server.Tabletop.Components
{
    /// <summary>
    ///     Component for marking an entity as currently playing a tabletop.
    /// </summary>
    [RegisterComponent, Friend(typeof(TabletopSystem))]
    public sealed class TabletopGamerComponent : Component
    {
        [DataField("tabletop")]
        public EntityUid Tabletop { get; set; } = EntityUid.Invalid;
    }
}
