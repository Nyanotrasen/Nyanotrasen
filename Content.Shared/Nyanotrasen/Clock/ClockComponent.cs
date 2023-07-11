namespace Content.Shared.Nyanotrasen.Clock
{
    [RegisterComponent]
    public sealed class ClockComponent : Component
    {
        [DataField("showSeconds")]
        public bool ShowSeconds = false;
    }
}
