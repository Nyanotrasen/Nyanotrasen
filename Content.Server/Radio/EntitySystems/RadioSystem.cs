using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Map;
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

        var name = TryComp(messageSource, out VoiceMaskComponent? mask) && mask.Enabled
            ? mask.VoiceName
            : MetaData(messageSource).EntityName;

        name = FormattedMessage.EscapeText(name);

        // most radios are relayed to chat, so lets parse the chat message beforehand
        var chat = new ChatMessage(
            ChatChannel.Radio,
            message,
            Loc.GetString("chat-radio-message-wrap", ("color", channel.Color), ("channel", $"\\[{channel.LocalizedName}\\]"), ("name", name), ("message", FormattedMessage.EscapeText(message))),
            EntityUid.Invalid);
        var chatMsg = new MsgChatMessage { Message = chat };
        var ev = new RadioReceiveEvent(message, messageSource, channel, chatMsg);

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var hasMicro = HasComp<RadioMicrophoneComponent>(radioSource);

        var speakerQuery = GetEntityQuery<RadioSpeakerComponent>();
        var radioQuery = AllEntityQuery<ActiveRadioComponent, TransformComponent>();
        var sentAtLeastOnce = false;
        while (radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.Channels.Contains(channel.ID))
                continue;

            if (!channel.LongRange && transform.MapID != sourceMapId)
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && (!hasMicro || !speakerQuery.HasComponent(receiver));
            if (needServer && !hasActiveServer)
                continue;
                
            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            RaiseLocalEvent(receiver, ref ev);
            sentAtLeastOnce = true;
        }
        if (!sentAtLeastOnce)
            _popup.PopupEntity(Loc.GetString("failed-to-send-message"), messageSource, messageSource, PopupType.MediumCaution);

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {message}");

        _replay.QueueReplayMessage(chat);
        _messages.Remove(message);
        */
    }

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}
