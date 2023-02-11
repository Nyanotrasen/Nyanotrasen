using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Damage;
using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Serialization;

namespace Content.Shared.Drone
{
    public class SharedDroneSystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<DroneComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        }

        private void OnInteractionAttempt(EntityUid uid, DroneComponent component, InteractionAttemptEvent args)
        {
            if (args.Target == null)
                return;

            if (TryComp<DamageableComponent>(args.Target, out var dmg) && dmg.DamageContainerID == "Biological")
                args.Cancel();

            if (HasComp<ItemComponent>(args.Target) && !HasComp<UnremoveableComponent>(args.Target)
                && !_tagSystem.HasAnyTag(args.Target.Value, "DroneUsable", "Trash"))
                args.Cancel();
        }

        [Serializable, NetSerializable]
        public enum DroneVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum DroneStatus : byte
        {
            Off,
            On
        }
    }
}
