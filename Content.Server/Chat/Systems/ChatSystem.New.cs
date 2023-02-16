namespace Content.Server.Chat.Systems
{
    public sealed partial class ChatSystem
    {
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

            // Allow last-minute cancellation of the chat message.
            var before = new BeforeEntityChatEvent(chat);
            RaiseLocalEvent(source, ref before, true);

            if (before.Cancelled)
                return false;

            // Send the actual message on a per-recipient basis, so they can do
            // their own individual handling.
            foreach (var (recipient, data) in chat.Recipients)
            {
                // TODO: Allow last-minute cancellation of the chat message at the recipient.

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
