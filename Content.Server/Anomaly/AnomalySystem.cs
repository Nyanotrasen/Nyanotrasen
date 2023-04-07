﻿using Content.Server.Anomaly.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Audio;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Materials;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles logic and interactions relating to <see cref="AnomalyComponent"/>
/// </summary>
public sealed partial class AnomalySystem : SharedAnomalySystem
{
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly MaterialStorageSystem _material = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public const float MinParticleVariation = 0.8f;
    public const float MaxParticleVariation = 1.2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AnomalyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AnomalyComponent, StartCollideEvent>(OnStartCollide);

        InitializePsionics();
        InitializeGenerator();
        InitializeScanner();
        InitializeVessel();
    }

    private void OnMapInit(EntityUid uid, AnomalyComponent component, MapInitEvent args)
    {
        component.NextPulseTime = Timing.CurTime + GetPulseLength(component) * 3; // longer the first time
        ChangeAnomalyStability(uid, Random.NextFloat(component.InitialStabilityRange.Item1 , component.InitialStabilityRange.Item2), component);
        ChangeAnomalySeverity(uid, Random.NextFloat(component.InitialSeverityRange.Item1, component.InitialSeverityRange.Item2), component);

        var particles = new List<AnomalousParticleType>
            { AnomalousParticleType.Delta, AnomalousParticleType.Epsilon, AnomalousParticleType.Zeta };
        component.SeverityParticleType = Random.PickAndTake(particles);
        component.DestabilizingParticleType = Random.PickAndTake(particles);
        component.WeakeningParticleType = Random.PickAndTake(particles);
    }

    private void OnShutdown(EntityUid uid, AnomalyComponent component, ComponentShutdown args)
    {
        EndAnomaly(uid, component);
    }

    private void OnStartCollide(EntityUid uid, AnomalyComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<AnomalousParticleComponent>(args.OtherFixture.Body.Owner, out var particleComponent))
            return;

        if (args.OtherFixture.ID != particleComponent.FixtureId)
            return;

        // small function to randomize because it's easier to read like this
        float VaryValue(float v) => v * Random.NextFloat(MinParticleVariation, MaxParticleVariation);

        if (particleComponent.ParticleType == component.DestabilizingParticleType)
        {
            ChangeAnomalyStability(uid, VaryValue(component.StabilityPerDestabilizingHit), component);
        }
        else if (particleComponent.ParticleType == component.SeverityParticleType)
        {
            ChangeAnomalySeverity(uid, VaryValue(component.SeverityPerSeverityHit), component);
        }
        else if (particleComponent.ParticleType == component.WeakeningParticleType)
        {
            ChangeAnomalyHealth(uid, VaryValue(component.HealthPerWeakeningeHit), component);
            ChangeAnomalyStability(uid, VaryValue(component.StabilityPerWeakeningeHit), component);
        }
    }

    /// <summary>
    /// Gets the amount of research points generated per second for an anomaly.
    /// </summary>
    /// <param name="anomaly"></param>
    /// <param name="component"></param>
    /// <returns>The amount of points</returns>
    public int GetAnomalyPointValue(EntityUid anomaly, AnomalyComponent? component = null)
    {
        if (!Resolve(anomaly, ref component, false))
            return 0;

        var multiplier = 1f;
        if (component.Stability > component.GrowthThreshold)
            multiplier = component.GrowingPointMultiplier; //more points for unstable

        //penalty of up to 50% based on health
        multiplier *= MathF.Pow(1.5f, component.Health) - 0.5f;

        var severityValue = 1 / (1 + MathF.Pow(MathF.E, -7 * (component.Severity - 0.5f)));

        return (int) ((component.MaxPointsPerSecond - component.MinPointsPerSecond) * severityValue * multiplier) + component.MinPointsPerSecond;
    }

    /// <summary>
    /// Gets the localized name of a particle.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public string GetParticleLocale(AnomalousParticleType type)
    {
        return type switch
        {
            AnomalousParticleType.Delta => Loc.GetString("anomaly-particles-delta"),
            AnomalousParticleType.Epsilon => Loc.GetString("anomaly-particles-epsilon"),
            AnomalousParticleType.Zeta => Loc.GetString("anomaly-particles-zeta"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateGenerator();
        UpdateVessels();
    }
}
