using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Kitchen.Components;
using Content.Shared.Botany;
using Content.Shared.Examine;
using Content.Shared.Random.Helpers;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Botany.Systems;

public sealed partial class BotanySystem
{
    public void InitializeSeeds()
    {
        SubscribeLocalEvent<SeedComponent, ExaminedEvent>(OnExamined);
    }

    public bool TryGetSeed(SeedComponent comp, [NotNullWhen(true)] out SeedData? seed)
    {
        if (comp.Seed != null)
        {
            seed = comp.Seed;
            return true;
        }

        if (comp.SeedId != null
            && _prototypeManager.TryIndex(comp.SeedId, out SeedPrototype? protoSeed))
        {
            seed = protoSeed;
            return true;
        }

        seed = null;
        return false;
    }

    public bool TryGetSeed(ProduceComponent comp, [NotNullWhen(true)] out SeedData? seed)
    {
        if (comp.Seed != null)
        {
            seed = comp.Seed;
            return true;
        }

        if (comp.SeedId != null
            && _prototypeManager.TryIndex(comp.SeedId, out SeedPrototype? protoSeed))
        {
            seed = protoSeed;
            return true;
        }

        seed = null;
        return false;
    }

    private void OnExamined(EntityUid uid, SeedComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryGetSeed(component, out var seed))
            return;

        args.PushMarkup(Loc.GetString($"seed-component-description", ("seedName", seed.DisplayName)));
        args.PushMarkup(Loc.GetString($"seed-component-plant-yield-text", ("seedYield", seed.Yield)));
        args.PushMarkup(Loc.GetString($"seed-component-plant-potency-text", ("seedPotency", seed.Potency)));
    }

    #region SeedPrototype prototype stuff

    public EntityUid SpawnSeedPacket(SeedData proto, EntityCoordinates transformCoordinates)
    {
        var seed = Spawn(proto.PacketPrototype, transformCoordinates);
        var seedComp = EnsureComp<SeedComponent>(seed);
        seedComp.Seed = proto;

        if (TryComp(seed, out SpriteComponent? sprite))
        {
            // TODO visualizer
            // SeedPrototype state will always be seed. Blame the spriter if that's not the case!
            sprite.LayerSetSprite(0, new SpriteSpecifier.Rsi(proto.PlantRsi, "seed"));
        }

        string val = Loc.GetString("botany-seed-packet-name", ("seedName", proto.Name), ("seedNoun", proto.Noun));
        MetaData(seed).EntityName = val;

        return seed;
    }

    public IEnumerable<EntityUid> AutoHarvest(SeedData proto, EntityCoordinates position, int yieldMod = 1)
    {
        if (position.IsValid(EntityManager) &&
            proto.ProductPrototypes.Count > 0)
            return GenerateProduct(proto, position, yieldMod);

        return Enumerable.Empty<EntityUid>();
    }

    public IEnumerable<EntityUid> Harvest(SeedData proto, EntityUid user, int yieldMod = 1)
    {
        if (proto.ProductPrototypes.Count == 0 || proto.Yield <= 0)
        {
            _popupSystem.PopupCursor(Loc.GetString("botany-harvest-fail-message"),
                Filter.Entities(user));
            return Enumerable.Empty<EntityUid>();
        }

        _popupSystem.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", proto.DisplayName)),
            Filter.Entities(user));
        return GenerateProduct(proto, Transform(user).Coordinates, yieldMod);
    }

    public IEnumerable<EntityUid> GenerateProduct(SeedData proto, EntityCoordinates position, int yieldMod = 1)
    {
        var totalYield = 0;
        if (proto.Yield > -1)
        {
            if (yieldMod < 0)
                totalYield = proto.Yield;
            else
                totalYield = proto.Yield * yieldMod;

            totalYield = Math.Max(1, totalYield);
        }

        var products = new List<EntityUid>();

        if (totalYield > 1 || proto.HarvestRepeat != HarvestType.NoRepeat)
            proto.Unique = false;
            
        for (var i = 0; i < totalYield; i++)
        {
            var product = _robustRandom.Pick(proto.ProductPrototypes);

            var entity = Spawn(product, position);
            entity.RandomOffset(0.25f);
            products.Add(entity);

            var produce = EnsureComp<ProduceComponent>(entity);

            produce.Seed = proto;
            ProduceGrown(entity, produce);

            if (TryComp<AppearanceComponent>(entity, out var appearance))
            {
                appearance.SetData(ProduceVisuals.Potency, proto.Potency);
            }

            if (proto.Mysterious)
            {
                var metaData = MetaData(entity);
                metaData.EntityName += "?";
                metaData.EntityDescription += " " + Loc.GetString("botany-mysterious-description-addon");
            }
        }

        return products;
    }

    public bool CanHarvest(SeedData proto, EntityUid? held = null)
    {
        return !proto.Ligneous || proto.Ligneous && held != null && HasComp<SharpComponent>(held);
    }

    #endregion
}
