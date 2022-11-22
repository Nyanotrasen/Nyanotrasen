using Content.Shared.Interaction;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Content.Server.Abilities.Psionics;
using Content.Server.Players;
using Robust.Shared.Random;
using Robust.Server.GameObjects;

namespace Content.Server.Soul
{
    public sealed class GolemSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _slotsSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        private const string CrystalSlot = "crystal_slot";
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SoulCrystalComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<GolemComponent, DispelledEvent>(OnDispelled);
        }

        private void OnAfterInteract(EntityUid uid, SoulCrystalComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<GolemComponent>(args.Target, out var golem))
                return;

            if (!TryComp<ItemSlotsComponent>(args.Target, out var slots))
                return;

            if (!TryComp<ActorComponent>(uid, out var actor))
                return;

            if (!_slotsSystem.TryGetSlot(args.Target.Value, CrystalSlot, out var crystalSlot, slots)) // does it not have a crystal slot?
                return;

            if (_slotsSystem.GetItemOrNull(args.Target.Value, CrystalSlot, slots) != null) // is the crystal slot occupied?
                return;

            // Toggle the lock and insert the crystal.
            _slotsSystem.SetLock(args.Target.Value, CrystalSlot, false, slots);
            _slotsSystem.TryInsert(args.Target.Value, CrystalSlot, uid, args.User, slots);
            _slotsSystem.SetLock(args.Target.Value, CrystalSlot, true, slots);

            actor.PlayerSession.ContentData()?.Mind?.TransferTo(args.Target.Value);

            if (TryComp<AppearanceComponent>(args.Target, out var appearance))
                _appearance.SetData(args.Target.Value, ToggleVisuals.Toggled, true, appearance);
        }

        private void OnDispelled(EntityUid uid, GolemComponent component, DispelledEvent args)
        {
            _slotsSystem.SetLock(uid, CrystalSlot, false);
            _slotsSystem.TryEject(uid, CrystalSlot, null, out var item);
            _slotsSystem.SetLock(uid, CrystalSlot, true);

            if (item == null)
                return;

            args.Handled = true;

            Vector2 direction = (_robustRandom.Next(-30, 30), _robustRandom.Next(-30, 30));
            _throwing.TryThrow(item.Value, direction, _robustRandom.Next(1, 10));

            if (TryComp<AppearanceComponent>(uid, out var appearance))
                _appearance.SetData(uid, ToggleVisuals.Toggled, false, appearance);

            if (!TryComp<ActorComponent>(uid, out var actor))
                return;

            actor.PlayerSession.ContentData()?.Mind?.TransferTo(item);
        }
    }
}
