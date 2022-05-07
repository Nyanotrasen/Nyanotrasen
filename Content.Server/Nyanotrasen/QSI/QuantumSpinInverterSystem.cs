using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Shared.Audio;

namespace Content.Server.QSI
{
    public sealed class QuantumSpinInverterSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popups = default!;
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
                _popups.PopupEntity(Loc.GetString("qsi-already-bonded"), uid, Filter.Entities(args.User));
                return;
            }

            if (args.Target == null || !TryComp<QuantumSpinInverterComponent>(args.Target, out var otherQSI))
                return;

            component.Partner = otherQSI.Owner;
            otherQSI.Partner = component.Owner;
            _popups.PopupEntity(Loc.GetString("qsi-bonded"), uid, Filter.Entities(args.User));
        }

        private void OnUseInHand(EntityUid uid, QuantumSpinInverterComponent component, UseInHandEvent args)
        {
            if (component.Partner == null)
                return;



            var destination = Transform((EntityUid) component.Partner).Coordinates;

            Transform(args.User).Coordinates = destination;
        }
    }
}
