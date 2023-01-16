using Content.Shared.Mobs.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Inventory;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Server.Popups;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Construction;
using Robust.Shared.Player;
using Robust.Shared.Physics.Components;

namespace Content.Server.Psionics.Glimmer;
/// <summary>
/// Fries tinfoil hats and cages
/// </summary>
public sealed class NoosphericFry : GlimmerEventSystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedGlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly GlimmerReactiveSystem _glimmerReactiveSystem = default!;
    [Dependency] private readonly AnchorableSystem _anchorableSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;

    public override string Prototype => "NoosphericFry";
    public override void Started()
    {
        base.Started();
        List<(EntityUid wearer, TinfoilHatComponent worn)> psionicList = new();
        foreach (var (psion, mobState) in EntityManager.EntityQuery<PsionicInsulationComponent, MobStateComponent>())
        {
            if (!_mobStateSystem.IsAlive(psion.Owner))
                continue;

            if (!_inventorySystem.TryGetSlotEntity(psion.Owner, "head", out var headItem))
                continue;

            if (!TryComp<TinfoilHatComponent>(headItem, out var tinfoil))
                continue;

            psionicList.Add((psion.Owner, tinfoil));
        }

        foreach (var pair in psionicList)
        {
            if (pair.worn.DestroyOnFry)
            {
                QueueDel(pair.worn.Owner);
                Spawn("Ash", Transform(pair.wearer).Coordinates);
                _popupSystem.PopupEntity(Loc.GetString("psionic-burns-up", ("item", pair.worn.Owner)), pair.wearer, Filter.Pvs(pair.worn.Owner), true, Shared.Popups.PopupType.MediumCaution);
                _audioSystem.Play("/Audio/Effects/lightburn.ogg", Filter.Pvs(pair.worn.Owner), pair.worn.Owner, true);
            } else
            {
                _popupSystem.PopupEntity(Loc.GetString("psionic-burn-resist", ("item", pair.worn.Owner)), pair.wearer, Filter.Pvs(pair.worn.Owner), true, Shared.Popups.PopupType.SmallCaution);
                _audioSystem.Play("/Audio/Effects/lightburn.ogg", Filter.Pvs(pair.worn.Owner), pair.worn.Owner, true);
            }

            DamageSpecifier damage = new();
            damage.DamageDict.Add("Heat", 2.5);
            damage.DamageDict.Add("Shock", 2.5);

            if (_glimmerSystem.Glimmer > 500 && _glimmerSystem.Glimmer < 750)
            {
                damage *= 2;
                if (TryComp<FlammableComponent>(pair.wearer, out var flammableComponent))
                {
                    flammableComponent.FireStacks += 1;
                    _flammableSystem.Ignite(pair.wearer, flammableComponent);
                }
            } else if (_glimmerSystem.Glimmer > 750)
            {
                damage *= 3;
                if (TryComp<FlammableComponent>(pair.wearer, out var flammableComponent))
                {
                    flammableComponent.FireStacks += 2;
                    _flammableSystem.Ignite(pair.wearer, flammableComponent);
                }
            }

            _damageableSystem.TryChangeDamage(pair.wearer, damage, true, true);
        }

        // for probers:
        foreach (var reactive in EntityQuery<SharedGlimmerReactiveComponent>())
        {
            if (!TryComp<PhysicsComponent>(reactive.Owner, out var physics))
                return;

            // shoot out three bolts of lighting...
            _glimmerReactiveSystem.BeamRandomNearProber(reactive.Owner, 3, 12);

            // try to anchor if we can
            if (!Transform(reactive.Owner).Anchored && _anchorableSystem.TileFree(Transform(reactive.Owner).Coordinates, physics))
                Transform(reactive.Owner).Anchored = true;

            if (!TryComp<ApcPowerReceiverComponent>(reactive.Owner, out var power))
                continue;

            // If it's been turned off, turn it back on.
            if (power.PowerDisabled)
                _powerReceiverSystem.TogglePower(reactive.Owner, false);
        }
    }
}
