using System.Numerics;

namespace Content.Client.Nyanotrasen.Clock
{
    [RegisterComponent]
    public sealed class AnalogueClockVisualsComponent : Component
    {
        /// Where we'll rotate around, in pixels.
        [DataField("origin")]
        public Vector2 Origin = new Vector2(-1, 9);
    }
}
