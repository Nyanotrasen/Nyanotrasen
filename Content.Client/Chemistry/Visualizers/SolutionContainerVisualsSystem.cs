﻿using Content.Shared.Chemistry;
using Robust.Client.GameObjects;

namespace Content.Client.Chemistry.Visualizers;

public sealed class SolutionContainerVisualsSystem : VisualizerSystem<SolutionContainerVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, SolutionContainerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<float>(uid, SolutionContainerVisuals.FillFraction, out var fraction, args.Component))
            return;

        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(component.Layer, out var fillLayer))
            return;

        fraction = Math.Clamp(fraction, 0f, 1f);

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
