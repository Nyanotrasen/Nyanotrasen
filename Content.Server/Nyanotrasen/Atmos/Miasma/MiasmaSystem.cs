using Content.Shared.MobState;
using Content.Shared.Damage;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Server.Body.Components;
using Robust.Shared.Physics;
using Robust.Shared.Containers;

namespace Content.Server.Atmos.Miasma
{
    public sealed class MiasmaSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (rotting, perishable) in EntityQuery<RottingComponent, PerishableComponent>())
            {
                if (!perishable.Progressing)
                    continue;

                if (TryComp<TemperatureComponent>(perishable.Owner, out var temp) && temp.CurrentTemperature < 274f)
                    continue;

                perishable.DeathAccumulator += frameTime;
                if (perishable.DeathAccumulator < perishable.RotAfter.TotalSeconds)
                    continue;

                perishable.RotAccumulator += frameTime;
                if (perishable.RotAccumulator < 1f)
                    continue;

                perishable.RotAccumulator -= 1f;

                DamageSpecifier damage = new();
                damage.DamageDict.Add("Blunt", 0.25); // Slowly accumulate enough to explode after like half an hour
                damage.DamageDict.Add("Cellular", 0.25); // Cloning rework might use this eventually

                _damageableSystem.TryChangeDamage(perishable.Owner, damage, true, true);

                if (!TryComp<FixturesComponent>(perishable.Owner, out var fixtures))
                    continue;

                var mass = 0f;

                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    mass += fixture.Mass;
                }

                var tileMix = _atmosphereSystem.GetTileMixture(Transform(perishable.Owner).Coordinates);
                if (tileMix != null)
                    tileMix.AdjustMoles(6, perishable.MolsPerSecondPerUnitMass * mass);
            }
        }


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PerishableComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<PerishableComponent, BeingGibbedEvent>(OnGibbed);
            SubscribeLocalEvent<NoRotContainerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<NoRotContainerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        }

        private void OnMobStateChanged(EntityUid uid, PerishableComponent component, MobStateChangedEvent args)
        {
            if (args.Component.IsDead())
                EnsureComp<RottingComponent>(uid);
        }

        private void OnGibbed(EntityUid uid, PerishableComponent component, BeingGibbedEvent args)
        {
                if (!TryComp<FixturesComponent>(uid, out var fixtures))
                    return;

                var mass = 0f;

                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    mass += fixture.Mass;
                }

                var molsToDump = (component.MolsPerSecondPerUnitMass * mass) * component.DeathAccumulator;
                var tileMix = _atmosphereSystem.GetTileMixture(Transform(uid).Coordinates);
                if (tileMix != null)
                    tileMix.AdjustMoles(6, molsToDump);
        }

        private void OnEntInserted(EntityUid uid, NoRotContainerComponent component, EntInsertedIntoContainerMessage args)
        {
            if (TryComp<PerishableComponent>(args.Entity, out var perishable))
                perishable.Progressing = false;
        }

        private void OnEntRemoved(EntityUid uid, NoRotContainerComponent component, EntRemovedFromContainerMessage args)
        {
            if (TryComp<PerishableComponent>(args.Entity, out var perishable))
                perishable.Progressing = true;
        }
    }
}
