namespace Content.Shared.SimpleStation14.Clothing
{
    [RegisterComponent]
    public sealed class ClothingGrantComponentComponent : Component
    {
        [DataField("component"), ViewVariables(VVAccess.ReadWrite)]
        public string? Component = null;
        [DataField("tag"), ViewVariables(VVAccess.ReadWrite)]
        public string? Tag = null;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsActive = false;
    }
}
