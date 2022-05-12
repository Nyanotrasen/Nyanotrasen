namespace Content.Server.Weapon.StunOnHit
{
    [RegisterComponent]
    public sealed class StunOnHitComponent : Component
    {
        public bool Enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; set; } = 1.5f;
    }
}
