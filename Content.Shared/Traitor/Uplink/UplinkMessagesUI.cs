using Robust.Shared.Serialization;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public sealed class UplinkBuyListingMessage : BoundUserInterfaceMessage
    {
        public string ItemId;

        public UplinkBuyListingMessage(string itemId)
        {
            ItemId = itemId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class UplinkRequestUpdateInterfaceMessage : BoundUserInterfaceMessage
    {
        public UplinkRequestUpdateInterfaceMessage()
        {

        }
    }

    [Serializable, NetSerializable]
    public sealed class UplinkTryWithdrawTC : BoundUserInterfaceMessage
    {
        public int TC;

        public UplinkTryWithdrawTC(int tc)
        {
            TC = tc;
        }
    }
}
