using System.Linq; // todo: remove this stinky LINQy
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Forensics;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Audio;

namespace Content.Server.Forensics
{
    public sealed class ForensicScannerSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<TargetScanSuccessfulEvent>(OnTargetScanSuccessful);
            SubscribeLocalEvent<ScanCancelledEvent>(OnScanCancelled);
        }

        private void OnScanCancelled(ScanCancelledEvent ev)
        {
            if (ev.Component == null)
                return;
            ev.Component.CancelToken = null;
        }

        private void OnTargetScanSuccessful(TargetScanSuccessfulEvent ev)
        {
            ev.Component.CancelToken = null;

            if (!TryComp<ForensicsComponent>(ev.Target, out var forensics))
              return;

            ev.Component.Fingerprints = forensics.Fingerprints.ToList();
            ev.Component.Fibers = forensics.Fibers.ToList();
            OpenUserInterface(ev.User, ev.Component);
        }

        private void OnAfterInteract(EntityUid uid, ForensicScannerComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null)
            {
                component.CancelToken.Cancel();
                component.CancelToken = null;
            }

            if (args.Target == null || !args.CanReach)
                return;

            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, component.ScanDelay, component.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetScanSuccessfulEvent(args.User, args.Target, component),
                BroadcastCancelledEvent = new ScanCancelledEvent(uid, component),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        private void OnAfterInteractUsing(EntityUid uid, ForensicScannerComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (!TryComp<ForensicPadComponent>(args.Used, out var pad))
                return;

            foreach (var fiber in component.Fibers)
            {
                if (fiber == pad.Sample)
                {
                    SoundSystem.Play("/Audio/Machines/Nuke/angry_beep.ogg", Filter.Pvs(uid), uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fiber"), uid, Filter.Entities(args.User));
                    return;
                }
            }

            foreach (var fingerprint in component.Fingerprints)
            {
                if (fingerprint == pad.Sample)
                {
                    SoundSystem.Play("/Audio/Machines/Nuke/angry_beep.ogg", Filter.Pvs(uid), uid);
                    _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-fingerprint"), uid, Filter.Entities(args.User));
                    return;
                }
            }
            SoundSystem.Play("/Audio/Machines/airlock_deny.ogg", Filter.Pvs(uid), uid);
             _popupSystem.PopupEntity(Loc.GetString("forensic-scanner-match-none"), uid, Filter.Entities(args.User));
        }
        private void OpenUserInterface(EntityUid user, ForensicScannerComponent component)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            var ui = _uiSystem.GetUi(component.Owner, ForensicScannerUiKey.Key);

            ui.Open(actor.PlayerSession);
            ui.SendMessage(new ForensicScannerUserMessage(component.Fingerprints, component.Fibers));
        }
        private sealed class ScanCancelledEvent : EntityEventArgs
        {
            public EntityUid Uid;
            public ForensicScannerComponent Component;

            public ScanCancelledEvent(EntityUid uid, ForensicScannerComponent component)
            {
                Uid = uid;
                Component = component;
            }
        }

        private sealed class TargetScanSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid? Target;
            public ForensicScannerComponent Component;
            public TargetScanSuccessfulEvent(EntityUid user, EntityUid? target, ForensicScannerComponent component)
            {
                User = user;
                Target = target;
                Component = component;
            }
        }
    }
}
