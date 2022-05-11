using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.ShrinkRay;
using Content.Server.Clothing.Components;
using Content.Server.Disposal.Unit.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Server.ShrinkRay
{
    public sealed class ShrinkRaySystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedShrinkRaySystem _sharedShrink = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShrinkRayProjectileComponent, StartCollideEvent>(OnStartCollide);
            SubscribeLocalEvent<ShrunkenComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<ShrunkenComponent, DamageModifyEvent>(OnDamageModify);
        }

        private Queue<EntityUid> RemQueue = new();
        public override void Update(float frameTime)
        {
            base.FrameUpdate(frameTime);
            foreach (var shrunken in RemQueue)
            {
                var shrunkenComponent = Comp<ShrunkenComponent>(shrunken);
                RaiseNetworkEvent(new SizeChangedEvent(shrunken, shrunkenComponent.OriginalScaleFactor, true));

                RemComp<ShrunkenComponent>(shrunken);
                RemComp<ShrunkenSpriteComponent>(shrunken);

                _sharedShrink.RemoveShrunken(shrunken);

            }
            RemQueue.Clear();

            foreach (var shrunken in EntityQuery<ShrunkenComponent>())
            {
                if (HasComp<BeingDisposedComponent>(shrunken.Owner)) /// yeah not dealing with that
                {
                    continue;
                }

                shrunken.Accumulator += frameTime;
                if (shrunken.Accumulator < shrunken.ShrinkTime.TotalSeconds)
                {
                    continue;
                }
                RemQueue.Enqueue(shrunken.Owner);
            }
        }

        private void OnStartCollide(EntityUid uid, ShrinkRayProjectileComponent component, StartCollideEvent args)
        {
            if (_tagSystem.HasAnyTag(args.OtherFixture.Body.Owner, "Structure", "Wall", "Window"))
                return;

            if (TryComp<ShrunkenComponent>(args.OtherFixture.Body.Owner, out var alreadyShrank) && alreadyShrank.ScaleFactor == component.ScaleFactor)
            {
                    alreadyShrank.Accumulator = 0;
                    return;
            }

            ShrunkenComponent shrunken = new();
            shrunken.ScaleFactor = component.ScaleFactor;
            shrunken.Owner = args.OtherFixture.Body.Owner;

            EntityManager.AddComponent<ShrunkenComponent>(args.OtherFixture.Body.Owner, shrunken, true);

            RaiseNetworkEvent(new SizeChangedEvent(args.OtherFixture.Body.Owner, component.ScaleFactor, false));

            if (TryComp<FixturesComponent>(args.OtherFixture.Body.Owner, out var fixtures))
            {
                double averageScale = ((component.ScaleFactor.X + component.ScaleFactor.Y) / 2);
                var massScale = Math.Pow(averageScale, 3); /// 3 dimensions
                shrunken.MassScale = massScale;
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    fixture.Shape.Radius *= (float) massScale;
                    fixture.Mass *= (float) massScale;
                }
            }
            if (component.ApplyItem)
            {
                if (!HasComp<ItemComponent>(args.OtherFixture.Body.Owner) && !HasComp<SharedItemComponent>(args.OtherFixture.Body.Owner)) // yes it will crash without both of these
                {
                    shrunken.ShouldHaveItemComp = false;
                    AddComp<ItemComponent>(args.OtherFixture.Body.Owner);
                }
            }
        }
        private void OnShutdown(EntityUid uid, ShrunkenComponent component, ComponentShutdown args)
        {
            if (!component.ShouldHaveItemComp)
            {
                RemComp<ItemComponent>(uid);
                if (_containerSystem.IsEntityOrParentInContainer(uid))
                {
                    if (_containerSystem.TryGetOuterContainer(uid, Transform(uid), out var container))
                    {
                        Transform(uid).AttachParentToContainerOrGrid(EntityManager);
                        Transform(uid).LocalPosition = Transform(container.Owner).LocalPosition;
                    }
                }
            }

            if (TryComp<FixturesComponent>(uid, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    fixture.Shape.Radius /= (float) component.MassScale;
                    fixture.Mass /= (float) component.MassScale;
                }
            }
        }

        private void OnDamageModify(EntityUid uid, ShrunkenComponent component, DamageModifyEvent args)
        {
            var multiplier = (1 / ((component.ScaleFactor.X + component.ScaleFactor.Y) / 2));
            DamageModifierSetPrototype actualMods = new();
            if (_prototypeManager.TryIndex<DamageModifierSetPrototype>("SizeChanged", out var mods))
            {
                foreach (var coef in mods.Coefficients)
                {
                    actualMods.Coefficients.Add(coef.Key, multiplier);
                }
                args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, actualMods);
            }
        }
    }
}
