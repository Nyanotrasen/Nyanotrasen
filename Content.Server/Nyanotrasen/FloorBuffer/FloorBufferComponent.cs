namespace Content.Server.FloorBuffer
{
    [RegisterComponent]
    public sealed class FloorBufferComponent : Component
    {
        [DataField("accumulator")]
        public float Accumulator = 0f;

        /// <summary>
        /// How many units per second this will absorb from puddles in the tile below it.
        /// </summary>
        [DataField("unitsPerSecond")]
        public float UnitsPerSecond = 15f;

        /// <summary>
        /// How often in seconds the drain checks for puddles around it.
        /// If the EntityQuery seems a bit unperformant this can be increased.
        /// <summary>
        [DataField("bufferFrequency")]
        public float BufferFrequency = 1f;
    }
}
