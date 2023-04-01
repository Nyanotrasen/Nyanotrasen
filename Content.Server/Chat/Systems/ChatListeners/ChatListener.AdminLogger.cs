using Robust.Server.GameObjects;
using Content.Server.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Radio;

namespace Content.Server.Chat.Systems
{
    /// <summary>
    /// This listener logs all relevant chat messages.
    /// </summary>
    public sealed class AdminLoggerListenerSystem : ChatListenerSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        public override void Initialize()
        {
            ListenAfter = new Type[] { typeof(SayListenerSystem) };
            EnabledListeners = EnabledListener.AfterTransform;

            base.Initialize();
        }

        public override void AfterTransform(ref EntityChatAfterTransformEvent args)
        {
            if (!HasComp<ActorComponent>(args.Chat.Source))
                // Don't bother logging non-player messages.
                return;

            var logString = new LogStringHandler(0, 0);

            if (args.Chat.ClaimedBy == typeof(SayListenerSystem))
                logString.AppendLiteral("Say");
            else if (args.Chat.ClaimedBy == typeof(EmoteListenerSystem))
                logString.AppendLiteral("Emote");
            else if (args.Chat.ClaimedBy == typeof(WhisperListenerSystem))
                logString.AppendLiteral("Whisper");
            else if (args.Chat.ClaimedBy == typeof(RadioListenerSystem))
                logString.AppendLiteral("Radio");

            logString.AppendLiteral(" from ");

            // The following is required so the log viewer can find and sort out player entities.
            logString.AppendFormatted(ToPrettyString(args.Chat.Source), null, "user");

            if (args.Chat.TryGetData<RadioChannelPrototype[]>(ChatDataRadio.RadioChannels, out var channels))
            {
                if (channels.Length == 1)
                    logString.AppendLiteral($" on channel {channels[0].LocalizedName}");
                else if (channels.Length == 2)
                    logString.AppendLiteral($" on channels {channels[0].LocalizedName} and {channels[1].LocalizedName}");
                else if (channels.Length > 2)
                {
                    logString.AppendLiteral(" on channels");

                    for (var i = 0; i < channels.Length - 1; ++i)
                        logString.AppendLiteral($" {channels[i].LocalizedName},");

                    logString.AppendLiteral($" and {channels[^1].LocalizedName}");
                }
            }

            if (args.Chat.TryGetData<string>(ChatDataRadio.OriginalRadioMessage, out var originalRadioMessage) &&
                args.Chat.Message != originalRadioMessage ||
                originalRadioMessage == null &&
                args.Chat.Message != args.Chat.OriginalMessage)
            {
                logString.AppendLiteral($", original: {args.Chat.OriginalMessage} || transformed: {args.Chat.Message}");
            }
            else
                logString.AppendLiteral($": {args.Chat.Message}");

            _adminLogger.Add(LogType.Chat, LogImpact.Low, ref logString);
        }
    }
}
