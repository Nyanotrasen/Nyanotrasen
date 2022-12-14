namespace Content.Server.RatKing
{
    [RegisterComponent]
    public sealed class RatServantComponent : Component
    {
        /// <summary>
        ///     The rat king this servant is serving.
        /// </summary>
        public EntityUid? RatKing;
    }
};
