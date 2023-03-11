using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Server.Popups;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Replays;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed class RadioSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, CanTransmitOnRadioEvent>(OnCanTransmit);
    }

    private void OnCanTransmit(EntityUid uid, IntrinsicRadioTransmitterComponent component, ref CanTransmitOnRadioEvent args)
    {
        if (args.Handled)
            return;

        if (component.Channels.IsSupersetOf(args.StringChannels))
        {
            args.CanTransmit = true;
            args.RadioSource = uid;
            args.Handled = true;
        }
    }

    [Obsolete("Use ChatSystem.TrySendRadio instead.")]
    public void SendRadioMessage(EntityUid source, string message, RadioChannelPrototype channel, EntityUid? radioSource = null)
    {
        _chatSystem.TrySendRadio(source, message, new RadioChannelPrototype[] { channel });
        /*
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var name = TryComp(source, out VoiceMaskComponent? mask) && mask.Enabled
            ? mask.VoiceName
            : MetaData(source).EntityName;

        name = FormattedMessage.EscapeText(name);

        // most radios are relayed to chat, so lets parse the chat message beforehand
        var chat = new ChatMessage(
            ChatChannel.Radio,
            message,
            Loc.GetString("chat-radio-message-wrap", ("color", channel.Color), ("channel", $"\\[{channel.LocalizedName}\\]"), ("name", name), ("message", FormattedMessage.EscapeText(message))),
            EntityUid.Invalid);
        var chatMsg = new MsgChatMessage { Message = chat };

        var ev = new RadioReceiveEvent(message, source, channel, chatMsg, radioSource);
        var attemptEv = new RadioReceiveAttemptEvent(message, source, channel, radioSource);
        var sentAtLeastOnce = false;

        foreach (var radio in EntityQuery<ActiveRadioComponent>())
        {
            var ent = radio.Owner;
            // TODO map/station/range checks?

            if (!radio.Channels.Contains(channel.ID))
                continue;

            RaiseLocalEvent(ent, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }
            sentAtLeastOnce = true;
            RaiseLocalEvent(ent, ev);
        }
        if (!sentAtLeastOnce)
            _popupSystem.PopupEntity(Loc.GetString("failed-to-send-message"), source, source, PopupType.MediumCaution);

        if (name != Name(source))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(source):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(source):user} on {channel.LocalizedName}: {message}");

        _replay.QueueReplayMessage(chat);
        _messages.Remove(message);
        */
    }
}
