using System.Linq;
using System.Text;
using Content.Server.Hands.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Forensics
{
    public sealed class ForensicsSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<ForensicsComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ForensicsComponent, GettingInteractedWithAttemptEvent>(OnGettingInteractedWithAttempt);
            SubscribeLocalEvent<ForensicsComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
            SubscribeLocalEvent<FingerprintComponent, ComponentInit>(OnInit);
        }

        private void OnInteractHand(EntityUid uid, ForensicsComponent component, InteractHandEvent args)
        {
            ApplyEvidence(args.User, args.Target);
        }

        private void OnGettingInteractedWithAttempt(EntityUid uid, ForensicsComponent component, GettingInteractedWithAttemptEvent args)
        {
            if (args.Target == null)
                return;

            ApplyEvidence(args.Uid, args.Target.Value);
        }

        private void OnGettingPickedUpAttempt(EntityUid uid, ForensicsComponent component, GettingPickedUpAttemptEvent args)
        {
            ApplyEvidence(args.User, args.Item);
        }


        private void OnInit(EntityUid uid, FingerprintComponent component, ComponentInit args)
        {
            component.Fingerprint = GenerateFingerprint();
        }

        private string GenerateFingerprint()
        {
            byte[] fingerprint = new byte[16];
            _random.NextBytes(fingerprint);
            return Convert.ToHexString(fingerprint);
        }

        private void ApplyEvidence(EntityUid user, EntityUid target)
        {
            var component = EnsureComp<ForensicsComponent>(target);
            if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves))
            {
                if (TryComp<FiberComponent>(gloves, out var fiber) && fiber.FiberDescription != null)
                    component.Fibers.Add(fiber.FiberDescription);
                if (HasComp<FingerprintMaskComponent>(gloves))
                    return;
            }
            if (TryComp<FingerprintComponent>(user, out var fingerprint))
                component.Fingerprints.Add(fingerprint.Fingerprint ?? "");
        }
    }
}
