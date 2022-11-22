using Content.Shared.Interaction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Toggleable;

namespace Content.Server.Soul
{
    public sealed class GolemSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        private const string CrystalSlot = "crystal_slot";
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SoulCrystalComponent, AfterInteractEvent>(OnAfterInteract);
        }

        private void OnAfterInteract(EntityUid uid, SoulCrystalComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<GolemComponent>(args.Target, out var golem))
                return;

            if (!TryComp<ItemSlotsComponent>(args.Target, out var slots))
                return;

            if (!_slotsSystem.TryGetSlot(args.Target.Value, CrystalSlot, out var crystalSlot, slots)) // does it not have a crystal slot?
                return;

            if (_slotsSystem.GetItemOrNull(args.Target.Value, CrystalSlot, slots) != null) // is the crystal slot occupied?
                return;

            // Toggle the lock and insert the crystal.
            _slotsSystem.SetLock(args.Target.Value, CrystalSlot, false, slots);
            _slotsSystem.TryInsert(args.Target.Value, CrystalSlot, uid, args.User, slots);
            _slotsSystem.SetLock(args.Target.Value, CrystalSlot, true, slots);

            if (TryComp<AppearanceComponent>(args.Target, out var appearance))
                _appearance.SetData(args.Target.Value, ToggleVisuals.Toggled, true, appearance);
        }
    }
}
