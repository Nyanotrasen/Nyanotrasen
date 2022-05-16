namespace Content.Server.Research.SophicScribe
{
    [RegisterComponent]
    public sealed class SophicScribeComponent : Component
    {
        public Queue<string> SpeechQueue = new();

        [DataField("accumulator")]
        public float Accumulator = 0f;

        [DataField("SpeechDelay")]
        public TimeSpan SpeechDelay = TimeSpan.FromSeconds(3.5);
    }
}
