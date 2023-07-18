using System.Numerics;
using Content.Shared.Traits.Assorted;
using Content.Client.Camera;
using Robust.Shared.Random;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Traits;

public sealed class ParacusiaSystem : SharedParacusiaSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly CameraRecoilSystem _camera = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ParacusiaComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ParacusiaComponent, PlayerDetachedEvent>(OnPlayerDetach);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalPlayer?.ControlledEntity is not EntityUid localPlayer)
            return;

        PlayParacusiaSounds(localPlayer);
    }

    private void OnComponentStartup(EntityUid uid, ParacusiaComponent component, ComponentStartup args)
    {
        component.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.MinTimeBetweenIncidents, component.MaxTimeBetweenIncidents));
    }

    private void OnPlayerDetach(EntityUid uid, ParacusiaComponent component, PlayerDetachedEvent args)
    {
        component.Stream?.Stop();
    }

    private void PlayParacusiaSounds(EntityUid uid)
    {
        if (!TryComp<ParacusiaComponent>(uid, out var paracusia))
            return;

        if (_timing.CurTime <= paracusia.NextIncidentTime)
            return;

        // Set the new time.
        var timeInterval = _random.NextFloat(paracusia.MinTimeBetweenIncidents, paracusia.MaxTimeBetweenIncidents);
        paracusia.NextIncidentTime += TimeSpan.FromSeconds(timeInterval);

        // Offset position where the sound is played
        var randomOffset =
            new Vector2
            (
                _random.NextFloat(-paracusia.MaxSoundDistance, paracusia.MaxSoundDistance),
                _random.NextFloat(-paracusia.MaxSoundDistance, paracusia.MaxSoundDistance)
            );

        var newCoords = Transform(uid).Coordinates.Offset(randomOffset);

        // Play the sound
        paracusia.Stream = _audio.PlayStatic(paracusia.Sounds, uid, newCoords);
    }

}
