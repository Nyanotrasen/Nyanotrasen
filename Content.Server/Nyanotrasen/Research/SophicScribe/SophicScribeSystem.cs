using Content.Shared.Interaction;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Radio;
using Content.Server.Chat.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Server.Radio.Components;
using Content.Server.Psionics.Glimmer;
using Content.Server.Abilities.Psionics;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.SophicScribe
{
    public sealed partial class SophicScribeSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var scribe in EntityQuery<SophicScribeComponent>())
            {
                if (_sharedGlimmerSystem.Glimmer == 0)
                    return; // yes, return. Glimmer value is global.

                scribe.Accumulator += frameTime;
                if (scribe.Accumulator > scribe.AnnounceInterval.TotalSeconds)
                    {
                        scribe.Accumulator -= (float) scribe.AnnounceInterval.TotalSeconds;

                        if (!TryComp<IntrinsicRadioTransmitterComponent>(scribe.Owner, out var radio)) return;

                        var message = Loc.GetString("glimmer-report", ("level", _sharedGlimmerSystem.Glimmer));
                        var channel = _prototypeManager.Index<RadioChannelPrototype>("Science");
                        _radioSystem.SendRadioMessage(scribe.Owner, message, channel);
                    }
            }
        }
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SophicScribeComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<GlimmerEventEndedEvent>(OnGlimmerEventEnded);
        }
        private void OnInteractHand(EntityUid uid, SophicScribeComponent component, InteractHandEvent args)
        {
            _chat.TrySendInGameICMessage(uid, Loc.GetString("glimmer-report", ("level", _sharedGlimmerSystem.Glimmer)), InGameICChatType.Speak, true);
        }

        private void OnGlimmerEventEnded(GlimmerEventEndedEvent args)
        {
            foreach (var scribe in EntityQuery<SophicScribeComponent>())
            {
                if (!TryComp<IntrinsicRadioTransmitterComponent>(scribe.Owner, out var radio)) return;

                // mind entities when...
                var speaker = scribe.Owner;
                if (TryComp<MindSwappedComponent>(scribe.Owner, out var swapped))
                {
                    speaker = swapped.OriginalEntity;
                }

                var message = Loc.GetString(args.Message, ("decrease", args.GlimmerBurned), ("level", _sharedGlimmerSystem.Glimmer));
                var channel = _prototypeManager.Index<RadioChannelPrototype>("Common");
                _radioSystem.SendRadioMessage(speaker, message, channel);
            }
        }
    }
}
