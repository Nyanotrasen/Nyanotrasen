using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions;
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

        public readonly IReadOnlyList<string> PsionicPowerPool = new[]
        {
            "PacificationPower",
            "MetapsionicPower",
            "DispelPower",
            "MassSleepPower",
            "PsionicInvisibilityPower",
            "MindSwapPower",
        };

        public void AddPsionics(EntityUid uid)
        {
            if (HasComp<PsionicComponent>(uid))
                return;

            if (!TryComp<MindComponent>(uid, out var mind) || mind.Mind?.UserId == null)
                return;

            if (!_playerManager.TryGetSessionById(mind.Mind.UserId.Value, out var client))
                return;

            if (!HasComp<GuaranteedPsionicComponent>(uid) && TryComp<ActorComponent>(uid, out var actor))
                _euiManager.OpenEui(new AcceptPsionicsEui(uid, this), client);
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
