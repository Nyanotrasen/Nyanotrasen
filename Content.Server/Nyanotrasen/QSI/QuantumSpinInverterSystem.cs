using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Server.Popups;
using Content.Server.Disposal.Unit.Components;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Server.QSI
{
    public sealed class QuantumSpinInverterSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<QuantumSpinInverterComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<QuantumSpinInverterComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnAfterInteract(EntityUid uid, QuantumSpinInverterComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (component.Partner != null)
            {
                _popups.PopupEntity(Loc.GetString("qsi-already-bonded"), uid, args.User);
                return;
            }

            if (args.Target == null || !TryComp<QuantumSpinInverterComponent>(args.Target, out var otherQSI))
                return;

            component.Partner = otherQSI.Owner;
            otherQSI.Partner = component.Owner;
            _popups.PopupEntity(Loc.GetString("qsi-bonded"), uid, args.User);
        }

        private void OnUseInHand(EntityUid uid, QuantumSpinInverterComponent component, UseInHandEvent args)
        {
            if (component.Partner == null)
                return;

            if (HasComp<BeingDisposedComponent>(component.Partner)) // This is way too glitchy
                return;

            SoundSystem.Play("/Audio/Effects/teleport_departure.ogg", Filter.Pvs(uid).RemoveWhereAttachedEntity(puid => puid == args.User), uid);
            if (_containerSystem.TryGetOuterContainer((EntityUid) component.Partner, Transform((EntityUid) component.Partner), out var container))
            {
                Transform(args.User).AttachToGridOrMap();
                Transform(args.User).Coordinates = Transform(container.Owner).Coordinates;
            } else
            {
                Transform(args.User).Coordinates = Transform((EntityUid) component.Partner).Coordinates;
            }


            SoundSystem.Play("/Audio/Effects/teleport_arrival.ogg", Filter.Pvs(uid), uid);

            EntityManager.QueueDeleteEntity(uid);
            EntityManager.QueueDeleteEntity((EntityUid) component.Partner);
        }
    }
}
