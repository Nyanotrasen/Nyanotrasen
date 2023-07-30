using Robust.Shared.Audio;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for tagging a mob as a nuke operative.
/// </summary>
[RegisterComponent]
public sealed class NukeOperativeComponent : Component
{
    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/nukeops_start.ogg");

    // Begin Nyano-code: prevent issues with mind swapping into a nuclear operative.
    [DataField("firstMindAdded")]
    public bool FirstMindAdded;
    // End Nyano-code.
}
