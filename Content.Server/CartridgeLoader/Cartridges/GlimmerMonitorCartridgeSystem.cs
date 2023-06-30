using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Psionics.Glimmer;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class GlimmerMonitorCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly GlimmerSystem _glimmer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GlimmerMonitorCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, GlimmerMonitorCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, GlimmerMonitorCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new GlimmerMonitorUiState(_glimmer.Glimmer);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
