using Content.Shared.Shipyard.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shipyard
{
    [NetSerializable, Serializable]
    public enum ShipyardConsoleUiKey : byte
    {
        //Not currently implemented. Could be used in the future to give other factions a variety of shuttle options.
        Shipyard,
        Syndicate
    }

    [UsedImplicitly]
    public abstract class SharedShipyardSystem : EntitySystem
    {
        
        
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedShipyardConsoleComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<SharedShipyardConsoleComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, SharedShipyardConsoleComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ShipyardConsoleComponentState state) return;
            component.AccessLevels = state.AccessLevels;
        }

        private void OnGetState(EntityUid uid, SharedShipyardConsoleComponent component, ref ComponentGetState args)
        {
            args.State = new ShipyardConsoleComponentState(component.AccessLevels);
        }

        [Serializable, NetSerializable]
        private sealed class ShipyardConsoleComponentState : ComponentState
        {
            public List<string> AccessLevels;

            public ShipyardConsoleComponentState(List<string> accessLevels)
            {
                AccessLevels = accessLevels;
            }
        }
    }
}
