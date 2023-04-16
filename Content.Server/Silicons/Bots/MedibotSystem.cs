using System.Diagnostics.CodeAnalysis;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Silicons;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.NPC.Components;
using Content.Server.Chemistry.EntitySystems;
using Robust.Shared.Physics.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Silicons.Bots
{
    public sealed class MedibotSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobs = default!;
        [Dependency] private readonly SolutionContainerSystem _solution = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly PhysicsSystem _physics = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MedibotComponent, InteractNoHandEvent>(PlayerInject);
            SubscribeLocalEvent<MedibotComponent, MedibotInjectDoAfterEvent>(OnDoAfter);
        }

        private void PlayerInject(EntityUid uid, MedibotComponent component, InteractNoHandEvent args)
        {
            if (args.Target == null)
                return;

            if (args.Target == uid)
                return;

            if (!SharedInjectChecks(uid, args.Target.Value, out var injectable))
                return;

            TryStartInject(uid, component, args.Target.Value, injectable);
        }

        private void OnDoAfter(EntityUid uid, MedibotComponent component, MedibotInjectDoAfterEvent args)
        {
            component.IsInjecting = false;

            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            _audioSystem.PlayPvs(component.InjectFinishSound, args.Args.Target.Value);
            _solution.TryAddReagent(args.Args.Target.Value, args.Solution, args.Drug, args.Amount, out var acceptedQuantity);
            _popups.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), args.Args.Target.Value, args.Args.Target.Value);
            EnsureComp<NPCRecentlyInjectedComponent>(args.Args.Target.Value);
            args.Handled = true;
        }

        public bool NPCStartInject(EntityUid uid, EntityUid target, MedibotComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (!SharedInjectChecks(uid, target, out var injectable))
                return false;

            return TryStartInject(uid, component, target, injectable);
        }

        private bool TryStartInject(EntityUid performer, MedibotComponent component, EntityUid target, Solution injectable)
        {
            if (component.IsInjecting)
                return false;

            if (!_blocker.CanInteract(performer, target))
                return false;

            if (!_interactionSystem.InRangeUnobstructed(performer, target, 2f))
                return false;

            // Hold still, please
            if (TryComp<PhysicsComponent>(target, out var physics) && physics.LinearVelocity.Length != 0f)
                return false;

            if (!ChooseDrug(target, component, out var drug, out var injectAmount))
            {
                _popups.PopupEntity(Loc.GetString("medibot-cannot-inject"), performer, performer, PopupType.SmallCaution);
                return false;
            }

            component.InjectTarget = target;

            _popups.PopupEntity(Loc.GetString("medibot-inject-receiver", ("bot", performer)), target, target, PopupType.Medium);
            _popups.PopupEntity(Loc.GetString("medibot-inject-actor", ("target", target)), performer, performer, PopupType.Medium);

            var ev = new MedibotInjectDoAfterEvent(injectable, drug, injectAmount);
            var args = new DoAfterArgs(performer, component.InjectDelay, ev, performer, target: target)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = false
            };

            component.IsInjecting = true;
            _doAfter.TryStartDoAfter(args);

            return true;
        }

        private bool SharedInjectChecks(EntityUid uid, EntityUid target, [NotNullWhen(true)] out Solution? injectable)
        {
            injectable = null;
            if (_mobs.IsDead(target))
                return false;

            if (HasComp<NPCRecentlyInjectedComponent>(target))
                return false;

            if (!_solution.TryGetInjectableSolution(target, out var injectableSol))
                return false;

            injectable = injectableSol;

            return true;
        }

        private bool ChooseDrug(EntityUid target, MedibotComponent component, out string drug, out float injectAmount, DamageableComponent? damage = null)
        {
            drug = "None";
            injectAmount = 0;
            if (!Resolve(target, ref damage))
                return false;

            if (damage.TotalDamage == 0)
                return false;

            if (_mobs.IsCritical(target))
            {
                drug = component.EmergencyMed;
                injectAmount = component.EmergencyMedInjectAmount;
                return true;
            }

            if (damage.TotalDamage <= 50)
            {
                drug = component.StandardMed;
                injectAmount = component.StandardMedInjectAmount;
                return true;
            }

            return false;
        }
    }
}
