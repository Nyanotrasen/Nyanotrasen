namespace Content.Server.Arachne
{
    [RegisterComponent]
    public sealed class CocoonComponent : Component
    {
        public bool WasReplacementAccent = false;

        public string OldAccent = "";
    }
}
