using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Server.Ghost.Components;
using Content.Shared.CCVar;

namespace Content.Server.Chat.Systems
{
    public sealed partial class ChatSystem
    {
        /// <summary>
        /// This method's purpose is to handle sanitizing messages from the IC speaking commands
        /// or to forward them to dead chat if the source is a ghost.
        /// </summary>
        /// <remarks>
        /// NPC code should directly use TrySendSay et cetera.
        /// </remarks>
        public void SendInGameICMessage(EntityUid source, string message, InGameICChatType desiredType, IConsoleShell? shell = null, IPlayerSession? player = null, bool force = false)
        {
            if (HasComp<GhostComponent>(source))
            {
                // Ghosts can only send dead chat messages, so we'll forward it to InGame OOC.
                TrySendInGameOOCMessage(source, message, InGameOOCChatType.Dead, false, shell, player);
                return;
            }

            if (!force && !CanSendInGame(message, shell, player))
                return;

            bool shouldCapitalize = (desiredType != InGameICChatType.Emote);
            bool shouldPunctuate = _configurationManager.GetCVar(CCVars.ChatPunctuation);

            message = SanitizeInGameICMessage(source, message, out var emoteStr, shouldCapitalize, shouldPunctuate);

            // This can happen if the entire string is sanitized out.
            if (string.IsNullOrEmpty(message))
                return;

            // Otherwise, send whatever type.
            switch (desiredType)
            {
                case InGameICChatType.Speak:
                    // There could be a radio message in it, so...
                    TrySendChatUnparsed(source, message);
                    // NOTE: This unparsed call won't be necessary when there are actual commands for every radio channel.
                    break;
                case InGameICChatType.Whisper:
                    TrySendWhisper(source, message);
                    break;
                case InGameICChatType.Emote:
                    TrySendEmote(source, message);
                    break;
                /* case InGameICChatType.Telepathic: */
                /*     _nyanoChatSystem.SendTelepathicChat(source, message); */
                /*     break; */
            }
        }

        /// <summary>
        /// Returns an enumerable of tuples containing player EntityUids and the distance from the source.
        /// </summary>
        public IEnumerable<(EntityUid, float)> GetPlayerEntitiesInRange(EntityUid source, float range)
        {
            var xforms = GetEntityQuery<TransformComponent>();

            var transformSource = xforms.GetComponent(source);
            var sourceCoords = transformSource.Coordinates;

            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} playerEntity)
                    continue;

                var transformEntity = xforms.GetComponent(playerEntity);

                if (sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance) &&
                    distance <= range)
                {
                    yield return (playerEntity, distance);
                }
            }
        }

        /// <summary>
        /// Try to send an unparsed chat message from an entity.
        /// </summary>
        public bool TrySendChatUnparsed(EntityUid source, string message)
        {
            var chat = new EntityChat(source, message);

            // Allow systems to parse the message and dictate which channel this message goes to.
            var parse = new EntityChatParseEvent(chat);
            RaiseLocalEvent(source, ref parse, true);

            if (!parse.Handled)
                return false;

            return TrySendChat(source, chat);
        }

        /// <summary>
        /// Try to send a parsed chat message from an entity.
        /// </summary>
        /// <remarks>
        /// This skips the EntityChatParseEvent phase.
        /// </remarks>
        public bool TrySendChat(EntityUid source, EntityChat chat)
        {
            // Allow systems to early cancel chat attempts.
            var attempt = new EntityChatAttemptEvent(chat);
            RaiseLocalEvent(source, ref attempt, true);

            if (attempt.Cancelled)
                return false;

            // Allow systems to select the recipients of the chat message.
            var getRecipients = new EntityChatGetRecipientsEvent(chat);
            RaiseLocalEvent(source, ref getRecipients, true);

            if (chat.Recipients.Count == 0)
                // No recipients were found, so it should be safe to discard
                // the entire attempt at this point.
                return false;

            // Allow systems to transform the chat message and source name, at the source.
            var doTransform = new EntityChatTransformEvent(chat);
            RaiseLocalEvent(source, ref doTransform, true);

            // All possible source transformation has been completed.
            // This is where other systems can pick up the chat for further handling.
            var afterTransform = new EntityChatAfterTransformEvent(chat);
            RaiseLocalEvent(source, ref afterTransform, true);

            // Send the actual message on a per-recipient basis, so they can do
            // their own individual handling.
            foreach (var (recipient, data) in chat.Recipients)
            {
                // Allow last-minute cancellation of the chat message at the recipient.
                var before = new BeforeEntityChatEvent(recipient, chat);
                RaiseLocalEvent(recipient, ref before, true);

                if (before.Cancelled)
                    continue;

                // Allow systems to transform the chat message and source name, at the destination.
                var doTransformRecipient = new GotEntityChatTransformEvent(recipient, chat, data);
                RaiseLocalEvent(recipient, ref doTransformRecipient, true);

                // Finally, send the chat event.
                var gotChat = new GotEntityChatEvent(recipient, chat, data);
                RaiseLocalEvent(recipient, ref gotChat, true);
            }

            return true;
        }
    }
}
