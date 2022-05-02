using System.Threading;
using Content.Shared.Verbs;
using Content.Shared.Inventory.Events;
using Content.Shared.MobState.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Server.Medical.Components;
using Content.Server.Clothing.Components;
using Content.Server.Popups;
using Content.Server.Body.Components;
using Content.Server.DoAfter;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Medical
{
    public sealed class StethoscopeSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StethoscopeComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<StethoscopeComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<WearingStethoscopeComponent, GetVerbsEvent<InnateVerb>>(AddStethoscopeVerb);
            SubscribeLocalEvent<ListenSuccessfulEvent>(OnListenSuccess);
            SubscribeLocalEvent<ListenCancelledEvent>(OnListenCancelled);
        }

        private void OnEquipped(EntityUid uid, StethoscopeComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.SlotFlags.HasFlag(args.SlotFlags))
                return;

            component.IsActive = true;

            EnsureComp<WearingStethoscopeComponent>(args.Equipee);
        }

        private void OnUnequipped(EntityUid uid, StethoscopeComponent component, GotUnequippedEvent args)
        {
            if (!component.IsActive)
                return;

            RemComp<WearingStethoscopeComponent>(args.Equipee);
            component.IsActive = false;
        }

        private void AddStethoscopeVerb(EntityUid uid, WearingStethoscopeComponent component, GetVerbsEvent<InnateVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!HasComp<MobStateComponent>(args.Target))
                return;

            if (component.CancelToken != null)
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    StartListening(uid, args.Target, component);
                },
                Text = Loc.GetString("stethoscope-verb"),
                IconTexture = "Clothing/Neck/Misc/stethoscope.rsi/icon.png",
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnListenSuccess(ListenSuccessfulEvent ev)
        {
            ev.Component.CancelToken = null;
            ExamineWithStethoscope(ev.User, ev.Target);
        }

        private void OnListenCancelled(ListenCancelledEvent ev)
        {
            if (ev.Component == null)
                return;
            ev.Component.CancelToken = null;
        }

        private void StartListening(EntityUid user, EntityUid target, WearingStethoscopeComponent comp)
        {
            comp.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, comp.Delay, comp.CancelToken.Token, target: target)
            {
                BroadcastFinishedEvent = new ListenSuccessfulEvent(user, target, comp),
                BroadcastCancelledEvent = new ListenCancelledEvent(user, comp),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }
        public void ExamineWithStethoscope(EntityUid user, EntityUid target)
        {
            if (!HasComp<RespiratorComponent>(target) || !TryComp<MobStateComponent>(target, out var mobState) || mobState.IsDead())
            {
                _popupSystem.PopupEntity(Loc.GetString("stethoscope-dead"), target, Filter.Entities(user));
                return;
            }

            if (!TryComp<DamageableComponent>(target, out var damage))
                return;

            if (!_prototypeManager.TryIndex<DamageGroupPrototype>("Airloss", out var airloss))
                return;

            if (!damage.Damage.TryGetDamageInGroup(airloss, out var totalAirloss))
                return;

            if (totalAirloss < 20)
            {
                _popupSystem.PopupEntity(Loc.GetString("stethoscope-normal"), target, Filter.Entities(user));
                return;
            }

            if (totalAirloss < 60)
            {
                _popupSystem.PopupEntity(Loc.GetString("stethoscope-hyper"), target, Filter.Entities(user));
                return;
            }

            if (totalAirloss < 80)
            {
                _popupSystem.PopupEntity(Loc.GetString("stethoscope-irregular"), target, Filter.Entities(user));
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("stethoscope-fucked"), target, Filter.Entities(user));
        }

        private sealed class ListenSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid Target;
            public WearingStethoscopeComponent Component;

            public ListenSuccessfulEvent(EntityUid user, EntityUid target, WearingStethoscopeComponent component)
            {
                User = user;
                Target = target;
                Component = component;
            }
        }

        private sealed class ListenCancelledEvent : EntityEventArgs
        {
            public EntityUid Uid;
            public WearingStethoscopeComponent Component;

            public ListenCancelledEvent(EntityUid uid, WearingStethoscopeComponent component)
            {
                Uid = uid;
                Component = component;
            }
        }

    }
}
