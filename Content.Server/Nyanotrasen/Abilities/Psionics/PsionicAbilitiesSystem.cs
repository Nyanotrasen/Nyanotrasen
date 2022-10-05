using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions;
using Content.Shared.Psionics.Glimmer;
using Content.Server.EUI;
using Content.Server.Psionics;
using Content.Server.Mind.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Abilities.Psionics
{
    public sealed class PsionicAbilitiesSystem : EntitySystem
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;

        public readonly IReadOnlyList<string> PsionicPowerPool = new[]
        {
            "MetapsionicPower",
            "DispelPower",
            "MassSleepPower",
            "PsionicInvisibilityPower",
            "MindSwapPower",
            "TelegnosisPower",
            "PsionicRegenerationPower",
        };

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicAwaitingPlayerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        }

        private void OnPlayerAttached(EntityUid uid, PsionicAwaitingPlayerComponent component, PlayerAttachedEvent args)
        {
            if (TryComp<PsionicBonusChanceComponent>(uid, out var bonus) && bonus.Warn == true)
                _euiManager.OpenEui(new AcceptPsionicsEui(uid, this), args.Player);
            else
                AddRandomPsionicPower(uid);
            RemCompDeferred<PsionicAwaitingPlayerComponent>(uid);
        }

        public void AddPsionics(EntityUid uid, bool warn = true)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            if (!TryComp<MindComponent>(uid, out var mind) || mind.Mind?.UserId == null)
            {
                EnsureComp<PsionicAwaitingPlayerComponent>(uid);
                return;
            }

            if (!_playerManager.TryGetSessionById(mind.Mind.UserId.Value, out var client))
                return;

            if (warn && TryComp<ActorComponent>(uid, out var actor))
                _euiManager.OpenEui(new AcceptPsionicsEui(uid, this), client);
            else
                AddRandomPsionicPower(uid);
        }

        public void AddPsionics(EntityUid uid, string powerComp)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            AddComp<PsionicComponent>(uid);

            var newComponent = (Component) _componentFactory.GetComponent(powerComp);
            newComponent.Owner = uid;

            EntityManager.AddComponent(uid, newComponent);
        }

        public void AddRandomPsionicPower(EntityUid uid)
        {
            AddComp<PsionicComponent>(uid);

            // uh oh, stinky!
            var newComponent = (Component) _componentFactory.GetComponent(_random.Pick(PsionicPowerPool));
            newComponent.Owner = uid;

            EntityManager.AddComponent(uid, newComponent);

            _glimmerSystem.AddToGlimmer(_random.Next(1, 5));
        }

        public void RemovePsionics(EntityUid uid)
        {
            if (!TryComp<PsionicComponent>(uid, out var psionic))
                return;

            foreach (var compName in PsionicPowerPool)
            {
                // component moment
                var comp = _componentFactory.GetComponent(compName);
                if (EntityManager.TryGetComponent(uid, comp.GetType(), out var psionicPower))
                    RemComp(uid, psionicPower);
            }
            if (psionic.PsionicAbility != null)
                _actionsSystem.RemoveAction(uid, psionic.PsionicAbility);

            _statusEffectsSystem.TryAddStatusEffect(uid, "Stutter", TimeSpan.FromMinutes(5), false, "StutteringAccent");

            RemComp<PsionicComponent>(uid);
        }
    }
}
