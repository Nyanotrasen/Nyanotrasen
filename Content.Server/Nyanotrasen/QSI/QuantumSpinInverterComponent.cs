namespace Content.Server.QSI
{
    [RegisterComponent]
    public sealed class QuantumSpinInverterComponent : Component
    {
        /// <summary>
        /// The other entity this is bonded to.
        /// Should be another QSI that's also bonded to this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public EntityUid? Partner = null;
    }
}
