using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Interaction;
using Content.Shared.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.ShrinkRay;
using Content.Server.Clothing.Components;
using Content.Server.Disposal.Unit.Components;
using Content.Server.PowerCell;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;

namespace Content.Server.ShrinkRay
{
    public sealed class ShrinkRaySystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedShrinkRaySystem _sharedShrink = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        [Dependency] private readonly PowerCellSystem _cellSystem = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ShrinkRayComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ShrinkRayProjectileComponent, StartCollideEvent>(OnStartCollide);
            SubscribeLocalEvent<ShrunkenComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<ShrunkenComponent, DamageModifyEvent>(OnDamageModify);
            SubscribeLocalEvent<ShrunkenComponent, PickupAttemptEvent>(OnPickupAttempt);
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

        private void OnAfterInteract(EntityUid uid, ShrinkRayComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (args.Target == null || _tagSystem.HasAnyTag((EntityUid) args.Target, "Structure", "Wall", "Window"))
                return;

            ApplySizeChange((EntityUid) args.Target, component.ScaleFactor, component.ApplyItem);
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
            ApplySizeChange(args.OtherFixture.Body.Owner, component.ScaleFactor, component.ApplyItem);
        }

        public void ApplySizeChange(EntityUid target, Vector2 scaleFactor, bool applyItem)
        {
            ShrunkenComponent shrunken = new();
            shrunken.ScaleFactor = scaleFactor;
            shrunken.Owner = target;

            if (TryComp<SpriteComponent>(target, out var sprite)
            && !HasComp<ShrunkenComponent>(target))
                shrunken.OriginalScaleFactor = sprite.Scale;

            EntityManager.AddComponent<ShrunkenComponent>(target, shrunken, true);

            RaiseNetworkEvent(new SizeChangedEvent(target, scaleFactor, false));

            double averageScale = ((scaleFactor.X + scaleFactor.Y) / 2);

            if (TryComp<FixturesComponent>(target, out var fixtures))
            {
                var massScale = Math.Pow(averageScale, 3); /// 3 dimensions
                shrunken.MassScale = massScale;
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    shrunken.OriginalRadii.Add(fixture, fixture.Shape.Radius);
                    fixture.Shape.Radius = (float) FixedPoint2.Max((fixture.Shape.Radius *= (float) massScale), 0.05);
                    fixture.Mass *= (float) massScale;
                }
            }

            if (TryComp<EyeComponent>(target, out var eye))
            {
                eye.Zoom *= (float) averageScale;
            }

            if (applyItem)
            {
                if (!HasComp<ItemComponent>(target) && !HasComp<SharedItemComponent>(target)) // yes it will crash without both of these
                {
                    shrunken.ShouldHaveItemComp = false;
                    var item = AddComp<ItemComponent>(target);
                    item.Size = 5;
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
                        Transform(uid).AttachToGridOrMap();
                        Transform(uid).LocalPosition = Transform(container.Owner).LocalPosition;
                    }
                }
            }

            if (TryComp<FixturesComponent>(uid, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    fixture.Mass /= (float) component.MassScale;
                    if (component.OriginalRadii.TryGetValue(fixture, out var radius))
                        fixture.Shape.Radius = component.OriginalRadii[fixture];
                }
            }

            if (TryComp<EyeComponent>(uid, out var eye))
            {
                eye.Zoom /= ((component.ScaleFactor.X + component.ScaleFactor.Y) / 2);
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

        private void OnPickupAttempt(EntityUid uid, ShrunkenComponent component, PickupAttemptEvent args)
        {
            if (HasComp<ShrunkenComponent>(args.Item))
                args.Cancel();
        }
    }
}
