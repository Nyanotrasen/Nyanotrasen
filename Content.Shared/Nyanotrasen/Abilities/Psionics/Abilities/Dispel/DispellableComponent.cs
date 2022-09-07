namespace Content.Shared.Abilities.Psionics
{
    [RegisterComponent]
    public sealed class DispellableComponent : Component
    {
        [DataField("delete")]
        public bool Delete = true;
    }
}