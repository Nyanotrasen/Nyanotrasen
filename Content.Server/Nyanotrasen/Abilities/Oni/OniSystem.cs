using Content.Server.Weapon.Melee;
using Content.Server.Tools;
using Content.Shared.Tools.Components;
using Robust.Shared.Containers;

namespace Content.Server.Abilities.Oni
{
    public sealed class OniSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<OniComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<OniComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
            SubscribeLocalEvent<OniComponent, MeleeHitEvent>(OnOniMeleeHit);
            SubscribeLocalEvent<HeldByOniComponent, MeleeHitEvent>(OnHeldMeleeHit);
        }

        private void OnEntInserted(EntityUid uid, OniComponent component, EntInsertedIntoContainerMessage args)
        {
            var heldComp = EnsureComp<HeldByOniComponent>(args.Entity);
            heldComp.Holder = uid;

            if (TryComp<ToolComponent>(args.Entity, out var tool) && _toolSystem.HasQuality(args.Entity, "Prying", tool))
                tool.SpeedModifier *= 1.66f;
        }

        private void OnEntRemoved(EntityUid uid, OniComponent component, EntRemovedFromContainerMessage args)
        {
            if (TryComp<ToolComponent>(args.Entity, out var tool) && _toolSystem.HasQuality(args.Entity, "Prying", tool))
                tool.SpeedModifier /= 1.66f;

            RemComp<HeldByOniComponent>(args.Entity);
        }

        private void OnOniMeleeHit(EntityUid uid, OniComponent component, MeleeHitEvent args)
        {
            args.ModifiersList.Add(component.MeleeModifiers);
        }

        private void OnHeldMeleeHit(EntityUid uid, HeldByOniComponent component, MeleeHitEvent args)
        {
            if (!TryComp<OniComponent>(component.Holder, out var oni))
                return;

            args.ModifiersList.Add(oni.MeleeModifiers);
        }
    }
}
