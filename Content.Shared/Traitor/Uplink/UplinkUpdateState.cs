using Robust.Shared.Serialization;

namespace Content.Shared.Traitor.Uplink
{
    [Serializable, NetSerializable]
    public sealed class UplinkUpdateState : BoundUserInterfaceState
    {
        public UplinkAccountData Account;
        public UplinkListingData[] Listings;

        public UplinkUpdateState(UplinkAccountData account, UplinkListingData[] listings)
        {
            Account = account;
            Listings = listings;
        }
    }
}
