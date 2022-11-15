namespace Content.Shared.Borgs
{
    [RegisterComponent]
    public sealed class LawsComponent : Component
    {
        [DataField("laws")]
        public HashSet<string> Laws = default!;
    }
}
