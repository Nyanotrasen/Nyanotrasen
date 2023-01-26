using Content.Client.EntityHealthBar.UI;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Client.Player;
using JetBrains.Annotations;
using Robust.Shared.Enums;

namespace Content.Client.EntityHealthBar
{
    [UsedImplicitly]
    public sealed class EntityHealthBarOverlay : Overlay
    {
        private readonly IEyeManager _eyeManager = default!;
        private readonly IPlayerManager _playerManager = default!;
        private readonly IEntityManager _entMan = default!;
        private readonly SharedTransformSystem _transformSys = default!;
        private readonly ShaderInstance _shader;
        private readonly Dictionary<EntityUid, EntityHealthBarGui> _guis = new();
        public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
        public EntityHealthBarOverlay(IEntityManager entMan, IPrototypeManager protoManager)
        {
            _entMan = entMan;
            _transformSys = _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

            _shader = protoManager.Index<ShaderPrototype>("unshaded").Instance();
        }

        /// <summary>
        /// Occlude other bars based on line-of-sight
        /// </summary>
        public bool CheckLOS = false;
        /// <summary>
        /// If this is not null, we'll only use this damage container.
        /// </summary>
        public string? DamageContainer;

        public void Reset()
        {
            foreach (var gui in _guis.Values)
            {
                gui.Dispose();
            }

            _guis.Clear();
        }
        protected override void Draw(in OverlayDrawArgs args)
        {
            var handle = args.WorldHandle;
            var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
            var spriteQuery = _entMan.GetEntityQuery<SpriteComponent>();
            var xformQuery = _entMan.GetEntityQuery<TransformComponent>();

            // If you use the display UI scale then need to set max(1f, displayscale) because 0 is valid.
            const float scale = 1f;
            var scaleMatrix = Matrix3.CreateScale(new Vector2(scale, scale));
            var rotationMatrix = Matrix3.CreateRotation(-rotation);
            handle.UseShader(_shader);

            args.WorldHandle.UseShader(_shader);

            foreach (var (mobState, damageable, theirxform) in _entMan.EntityQuery<MobStateComponent, DamageableComponent, TransformComponent>())
            {
                var entity = mobState.Owner;

                if (DamageContainer != null && damageable.DamageContainerID != DamageContainer)
                    continue;

                if (args.MapId != theirxform.MapID)
                {
                    RemoveGUIFromEntity(entity);
                    continue;
                }

                if (!_guis.ContainsKey(entity))
                {
                    var gui = new EntityHealthBarGui(entity);
                    _guis.Add(entity, gui);
                }
            }
            handle.UseShader(null);
            handle.SetTransform(Matrix3.Identity);
        }

        private void RemoveGUIFromEntity(EntityUid entity)
        {
            if (_guis.TryGetValue(entity, out var oldGui))
            {
                _guis.Remove(entity);
                oldGui.Dispose();
            }
        }
    }
}
