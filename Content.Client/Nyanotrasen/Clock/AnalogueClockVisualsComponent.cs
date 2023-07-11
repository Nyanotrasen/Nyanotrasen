namespace Content.Client.Nyanotrasen.Clock
{
    [RegisterComponent]
    public sealed class AnalogueClockVisualsComponent : Component
    {
        /// Where we'll rotate around, in pixels.
        [DataField("origin")]
        public Vector2 Origin = (-1, 9);
    }
}
