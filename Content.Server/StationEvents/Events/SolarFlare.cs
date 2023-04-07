using System.Linq;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Radio.Components;
using Content.Server.Radio;
using Robust.Shared.Random;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Server.StationEvents.Events;

public sealed class SolarFlare : StationEventSystem
{
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;

    public override string Prototype => "SolarFlare";

    private SolarFlareEventRuleConfiguration _event = default!;
    private float _effectTimer = 0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioSendAttempt);
    }

    public override void Added()
    {
        base.Added();

        if (Configuration is not SolarFlareEventRuleConfiguration ev)
            return;

        _event = ev;
        _event.EndAfter = RobustRandom.Next(ev.MinEndAfter, ev.MaxEndAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!RuleStarted)
            return;

        _effectTimer -= frameTime;
        if (_effectTimer < 0)
        {
            _effectTimer += 1;
            var lightQuery = EntityQueryEnumerator<PoweredLightComponent>();
            while (lightQuery.MoveNext(out var uid, out var light))
            {
                if (RobustRandom.Prob(_event.LightBreakChancePerSecond))
                    _poweredLight.TryDestroyBulb(uid, light);
            }
            var airlockQuery = EntityQueryEnumerator<AirlockComponent, DoorComponent>();
            while (airlockQuery.MoveNext(out var uid, out var airlock, out var door))
            {
                if (airlock.AutoClose && RobustRandom.Prob(_event.DoorToggleChancePerSecond))
                    _door.TryToggleDoor(uid, door);
            }
        }

        if (Elapsed > _event.EndAfter)
        {
            ForceEndSelf();
            return;
        }
    }

    private void OnRadioSendAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (RuleStarted &&
            args.Chat.TryGetData<RadioChannelPrototype[]>(ChatDataRadio.RadioChannels, out var radioChannels))
        {
            var stringRadioChannels = from channel in radioChannels select channel.ID;
            EntityUid? radioSource = args.Chat.GetData<EntityUid>(ChatDataRadio.RadioSource);

            if (_event.AffectedChannels.IsSupersetOf(stringRadioChannels))
                if (!_event.OnlyJamHeadsets || (HasComp<HeadsetComponent>(args.RadioReceiver) || HasComp<HeadsetComponent>(radioSource)))
                    args.Cancelled = true;
        }
    }
}
