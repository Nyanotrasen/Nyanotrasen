using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost.Components;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.MobState.Components;
using Robust.Server.Player;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        public const float PresetFailedCooldownIncrease = 30f;

        private GamePresetPrototype? _preset;

        private bool StartPreset(IPlayerSession[] origReadyPlayers, bool force)
        {
            var startAttempt = new RoundStartAttemptEvent(origReadyPlayers, force);
            RaiseLocalEvent(startAttempt);

            if (!startAttempt.Cancelled)
                return true;

            var presetTitle = _preset != null ? Loc.GetString(_preset.ModeTitle) : string.Empty;

            void FailedPresetRestart()
            {
                SendServerMessage(Loc.GetString("game-ticker-start-round-cannot-start-game-mode-restart",
                    ("failedGameMode", presetTitle)));
                RestartRound();
                DelayStart(TimeSpan.FromSeconds(PresetFailedCooldownIncrease));
            }

            if (_configurationManager.GetCVar(CCVars.GameLobbyFallbackEnabled))
            {
                var oldPreset = _preset;
                ClearGameRules();
                SetGamePreset(_configurationManager.GetCVar(CCVars.GameLobbyFallbackPreset));
                AddGamePresetRules();
                StartGamePresetRules();

                startAttempt.Uncancel();
                RaiseLocalEvent(startAttempt);

                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("game-ticker-start-round-cannot-start-game-mode-fallback",
                        ("failedGameMode", presetTitle),
                        ("fallbackMode", Loc.GetString(_preset!.ModeTitle))));

                if (startAttempt.Cancelled)
                {
                    FailedPresetRestart();
                    return false;
                }

                RefreshLateJoinAllowed();
            }
            else
            {
                FailedPresetRestart();
                return false;
            }

            return true;
        }

        private void InitializeGamePreset()
        {
            SetGamePreset(_configurationManager.GetCVar(CCVars.GameLobbyDefaultPreset));
        }

        public void SetGamePreset(GamePresetPrototype preset, bool force = false)
        {
            // Do nothing if this game ticker is a dummy!
            if (DummyTicker)
                return;

            _preset = preset;
            UpdateInfoText();

            if (force)
            {
                StartRound(true);
            }
        }

        public void SetGamePreset(string preset, bool force = false)
        {
            var proto = FindGamePreset(preset);
            if(proto != null)
                SetGamePreset(proto, force);
        }

        public GamePresetPrototype? FindGamePreset(string preset)
        {
            if (_prototypeManager.TryIndex(preset, out GamePresetPrototype? presetProto))
                return presetProto;

            foreach (var proto in _prototypeManager.EnumeratePrototypes<GamePresetPrototype>())
            {
                foreach (var alias in proto.Alias)
                {
                    if (preset.Equals(alias, StringComparison.InvariantCultureIgnoreCase))
                        return proto;
                }
            }

            return null;
        }

        public bool TryFindGamePreset(string preset, [NotNullWhen(true)] out GamePresetPrototype? prototype)
        {
            prototype = FindGamePreset(preset);

            return prototype != null;
        }

        private bool AddGamePresetRules()
        {
            if (DummyTicker || _preset == null)
                return false;

            foreach (var rule in _preset.Rules)
            {
                if (!_prototypeManager.TryIndex(rule, out GameRulePrototype? ruleProto))
                    continue;

                AddGameRule(ruleProto);
            }

            return true;
        }

        private void StartGamePresetRules()
        {
            foreach (var rule in _addedGameRules)
            {
                StartGameRule(rule);
            }
        }

        public bool OnGhostAttempt(Mind.Mind mind, bool canReturnGlobal)
        {
            var handleEv = new GhostAttemptHandleEvent(mind, canReturnGlobal);
            RaiseLocalEvent(handleEv);

            // Something else has handled the ghost attempt for us! We return its result.
            if (handleEv.Handled)
                return handleEv.Result;

            var playerEntity = mind.CurrentEntity;

            var entities = IoCManager.Resolve<IEntityManager>();
            if (entities.HasComponent<GhostComponent>(playerEntity))
                return false;

            if (mind.VisitingEntity != default)
            {
                mind.UnVisit();
            }

            var position = playerEntity is {Valid: true}
                ? Transform(playerEntity.Value).Coordinates
                : GetObserverSpawnPoint();

            // Ok, so, this is the master place for the logic for if ghosting is "too cheaty" to allow returning.
            // There's no reason at this time to move it to any other place, especially given that the 'side effects required' situations would also have to be moved.
            // + If CharacterDeadPhysically applies, we're physically dead. Therefore, ghosting OK, and we can return (this is critical for gibbing)
            //   Note that we could theoretically be ICly dead and still physically alive and vice versa.
            //   (For example, a zombie could be dead ICly, but may retain memories and is definitely physically active)
            // + If we're in a mob that is critical, and we're supposed to be able to return if possible,
            //   we're succumbing - the mob is killed. Therefore, character is dead. Ghosting OK.
            //   (If the mob survives, that's a bug. Ghosting is kept regardless.)
            var canReturn = canReturnGlobal && mind.CharacterDeadPhysically;

            if (canReturnGlobal && TryComp(playerEntity, out MobStateComponent? mobState))
            {
                if (mobState.IsCritical())
                {
                    canReturn = true;

                    //todo: what if they dont breathe lol
                    //cry deeply
                    DamageSpecifier damage = new(_prototypeManager.Index<DamageTypePrototype>("Asphyxiation"), 200);
                    _damageable.TryChangeDamage(playerEntity, damage, true);
                }
            }

            var ghost = Spawn("MobObserver", position.ToMap(entities));

            // Try setting the ghost entity name to either the character name or the player name.
            // If all else fails, it'll default to the default entity prototype name, "observer".
            // However, that should rarely happen.
            var meta = MetaData(ghost);
            if(!string.IsNullOrWhiteSpace(mind.CharacterName))
                meta.EntityName = mind.CharacterName;
            else if (!string.IsNullOrWhiteSpace(mind.Session?.Name))
                meta.EntityName = mind.Session.Name;

            var ghostComponent = Comp<GhostComponent>(ghost);

            if (mind.TimeOfDeath.HasValue)
            {
                ghostComponent.TimeOfDeath = mind.TimeOfDeath!.Value;
            }

            _ghosts.SetCanReturnToBody(ghostComponent, canReturn);

            if (canReturn)
                mind.Visit(ghost);
            else
                mind.TransferTo(ghost);
            return true;
        }
    }

    public sealed class GhostAttemptHandleEvent : HandledEntityEventArgs
    {
        public Mind.Mind Mind { get; }
        public bool CanReturnGlobal { get; }
        public bool Result { get; set; }

        public GhostAttemptHandleEvent(Mind.Mind mind, bool canReturnGlobal)
        {
            Mind = mind;
            CanReturnGlobal = canReturnGlobal;
        }
    }
}
