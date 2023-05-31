namespace Content.Server.Fugitive
{
    [RegisterComponent]
    public sealed class FugitiveComponent : Component
    {
        [DataField("firstMindAdded")]
        public bool FirstMindAdded = false;
    }
}
