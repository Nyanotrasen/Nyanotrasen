namespace Content.Server.Holosign
{
    [RegisterComponent]
    public sealed class HolosignProjectorComponent : Component
    {
        [ViewVariables]
        [DataField("charges")]
        public int Charges = 6;

        [ViewVariables(VVAccess.ReadWrite)]
        public int CurrentCharges = 6;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("signProto")]
        public string SignProto = "HolosignWetFloor";

        [ViewVariables(VVAccess.ReadWrite)]
        public float Accumulator = 0f;

        [ViewVariables(VVAccess.ReadWrite)]

        public TimeSpan RechargeTime = TimeSpan.FromSeconds(30);
    }
}
