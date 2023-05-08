using Content.Shared.Examine;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles permanent blindness, both the examine and the actual effect.
/// </summary>
public sealed class PermanentBlindnessSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly BlindableSystem _blinding = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PermanentBlindnessComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PermanentBlindnessComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PermanentBlindnessComponent, CanSeeAttemptEvent>(OnTrySee);
        SubscribeLocalEvent<PermanentBlindnessComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, PermanentBlindnessComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && !_net.IsClient)
        {
            args.PushMarkup(Loc.GetString("permanent-blindness-trait-examined", ("target", Identity.Entity(uid, EntityManager))));
        }
    }

    private void OnShutdown(EntityUid uid, PermanentBlindnessComponent component, ComponentShutdown args)
    {
        _blinding.UpdateIsBlind(uid);
    }

    private void OnStartup(EntityUid uid, PermanentBlindnessComponent component, ComponentStartup args)
    {
        _blinding.UpdateIsBlind(uid);

        // give blind gear (i.e. cane)
        if (!TryComp(uid, out HandsComponent? handsComponent))
            return;

        var coords = EntityManager.GetComponent<TransformComponent>(uid).Coordinates;
        var inhandEntity = EntityManager.SpawnEntity(component.BlindGear, coords);
        _sharedHandsSystem.TryPickup(uid, inhandEntity, checkActionBlocker: false,
            handsComp: handsComponent);

    }

    private void OnTrySee(EntityUid uid, PermanentBlindnessComponent component, CanSeeAttemptEvent args)
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }
}
