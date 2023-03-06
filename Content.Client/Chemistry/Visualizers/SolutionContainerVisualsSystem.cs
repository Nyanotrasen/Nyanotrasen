﻿using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Chemistry.Visualizers;

public sealed class SolutionContainerVisualsSystem : VisualizerSystem<SolutionContainerVisualsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SolutionContainerVisualsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SolutionContainerVisualsComponent component, MapInitEvent args)
    {
        var meta = MetaData(uid);
        component.InitialName = meta.EntityName;
        component.InitialDescription = meta.EntityDescription;
    }

    protected override void OnAppearanceChange(EntityUid uid, SolutionContainerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<float>(uid, SolutionContainerVisuals.FillFraction, out var fraction, args.Component))
            return;

        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(component.FillLayer, out var fillLayer))
            return;

        fraction = Math.Clamp(fraction, 0f, 1f);

        if (component.Metamorphic)
        {
            if (args.Sprite.LayerMapTryGet(component.BaseLayer, out var baseLayer))
            {
                var hasOverlay = args.Sprite.LayerMapTryGet(component.OverlayLayer, out var overlayLayer);

                if (AppearanceSystem.TryGetData<string>(uid, SolutionContainerVisuals.BaseOverride,
                        out var baseOverride,
                        args.Component))
                {
                    _prototype.TryIndex<ReagentPrototype>(baseOverride, out var reagentProto);

                    var metadata = MetaData(uid);

                    if (reagentProto?.MetamorphicSprite is { } sprite)
                    {
                        args.Sprite.LayerSetSprite(baseLayer, sprite);
                        args.Sprite.LayerSetVisible(fillLayer, false);
                        if (hasOverlay)
                            args.Sprite.LayerSetVisible(overlayLayer, false);
                        metadata.EntityName = Loc.GetString(component.MetamorphicNameFull,
                            ("name", reagentProto.LocalizedName));
                        metadata.EntityDescription = reagentProto.LocalizedDescription;
                        return;
                    }
                    else
                    {
                        if (hasOverlay)
                            args.Sprite.LayerSetVisible(overlayLayer, true);
                        args.Sprite.LayerSetSprite(baseLayer, component.MetamorphicDefaultSprite);
                        metadata.EntityName = component.InitialName;
                        metadata.EntityDescription = component.InitialDescription;
                    }
                }
            }
        }

        var closestFillSprite = (int) Math.Round(fraction * component.MaxFillLevels);

        if (closestFillSprite > 0)
        {
            if (component.FillBaseName == null)
                return;

            args.Sprite.LayerSetVisible(fillLayer, true);

            var stateName = component.FillBaseName + closestFillSprite;
            args.Sprite.LayerSetState(fillLayer, stateName);

            if (component.ChangeColor && AppearanceSystem.TryGetData<Color>(uid, SolutionContainerVisuals.Color, out var color, args.Component))
                args.Sprite.LayerSetColor(fillLayer, color);
        }
        else
        {
            if (component.EmptySpriteName == null)
                args.Sprite.LayerSetVisible(fillLayer, false);
            else
            {
                args.Sprite.LayerSetState(fillLayer, component.EmptySpriteName);
                if (component.ChangeColor)
                    args.Sprite.LayerSetColor(fillLayer, component.EmptySpriteColor);
            }
        }


    }
}
