using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class SayCommand : IConsoleCommand
    {
        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not IPlayerSession player)
            {
                shell.WriteError("This command cannot be run from the server.");
                return;
            }

            if (player.Status != SessionStatus.InGame)
                return;

            if (player.AttachedEntity is not {} playerEntity)
            {
                shell.WriteError("You don't have an entity!");
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var chat = new EntityChat(playerEntity, message);

            // Allow systems to parse the message and dictate which channel this message goes to.
            var parse = new EntityChatParseEvent(chat);
            entityManager.EventBus.RaiseLocalEvent(playerEntity, ref parse, true);

            if (!parse.Handled)
                return;

            // -- Below would be part of ChatListener systems.

            // Allow systems to early cancel chat attempts.
            var attempt = new EntityChatAttemptEvent(chat);
            entityManager.EventBus.RaiseLocalEvent(playerEntity, ref attempt, true);

            if (attempt.Cancelled)
                return;

            // Allow systems to select the recipients of the chat message.
            var getRecipients = new EntityChatGetRecipientsEvent(chat);
            entityManager.EventBus.RaiseLocalEvent(playerEntity, ref getRecipients, true);

            // No recipients were found, so it should be safe to discard the entire attempt at this point.
            if (chat.Recipients.Count == 0)
                return;

            // Allow systems to transform the chat message and source name, at the source.
            var doTransform = new EntityChatTransformEvent(chat);
            entityManager.EventBus.RaiseLocalEvent(playerEntity, ref doTransform, true);

            // Allow last-minute cancellation of the chat message.
            var before = new BeforeEntityChatEvent(chat);
            entityManager.EventBus.RaiseLocalEvent(playerEntity, ref before, true);

            if (before.Cancelled)
                return;

            // TODO: allow systems to cancel/transform per recipient?

            // Send the actual message on a per-recipient basis, so they can do
            // their own individual handling.
            foreach (var recipient in chat.Recipients)
            {
                var gotChat = new GotEntityChatEvent(recipient, chat);
                entityManager.EventBus.RaiseLocalEvent(recipient, ref gotChat, true);
            }
        }
    }
}
