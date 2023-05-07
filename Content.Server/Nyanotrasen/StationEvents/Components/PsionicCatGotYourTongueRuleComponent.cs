using Robust.Shared.Audio;
using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(PsionicCatGotYourTongueRule))]
public sealed class PsionicCatGotYourTongueRuleComponent : Component
{
    [DataField("minDuration")]
    public TimeSpan MinDuration = TimeSpan.FromSeconds(20);

    [DataField("maxDuration")]
    public TimeSpan MaxDuration = TimeSpan.FromSeconds(80);

    [DataField("sound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Voice/Felinid/cat_scream1.ogg");
}
