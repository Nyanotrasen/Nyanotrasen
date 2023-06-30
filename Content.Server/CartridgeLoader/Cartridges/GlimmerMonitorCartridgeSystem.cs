﻿using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Server.Psionics.Glimmer;
using System.Linq;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class GlimmerMonitorCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly PassiveGlimmerReductionSystem _glimmerReductionSystem = default!;

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

        var state = new GlimmerMonitorUiState(FormatGlimmerValues(_glimmerReductionSystem.GlimmerValues));
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    /// <summary>
    /// We don't want to return more than 15 elements.
    /// </summary>
    private List<int> FormatGlimmerValues(List<int> glimmerValues)
    {
        List<int> returnList;

        if (glimmerValues.Count <= 15)
        {
            returnList = glimmerValues;
        }
        else
        {
            returnList = glimmerValues.Skip(glimmerValues.Count - 15).ToList();
        }

        Logger.Error("Return list: ");
        foreach (var value in returnList)
        {
            Logger.Error(value.ToString());
        }

        return returnList;
    }
}
